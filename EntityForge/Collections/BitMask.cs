using CommunityToolkit.HighPerformance;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace EntityForge.Collections
{
    public sealed class BitMask : IEquatable<BitMask>
    {
        private long[] bits;

        public ReadOnlySpan<long> Bits => bits;
        public BitMask()
        {
            bits = new long[1];
        }

        public bool IsAllZeros()
        {
            return Bits.IndexOfAnyExcept(0) == -1; //TODO: .Net 8 Replace with !ContainsAnyExcept(0)
        }

        public bool HasAnySet()
        {
            return Bits.IndexOfAnyExcept(0) != -1; //TODO: .Net 8 Replace with ContainsAnyExcept(0)
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsSet(int index)
        {
            int bitIndex = index >>> 6;
            if (bitIndex < bits.Length)
            {
                int remainder = index & (63);
                return (bits[bitIndex] & (1L << remainder)) != 0;
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetBit(int index)
        {
            int bitIndex = index >>> 6;
            ResizeIfNeeded(bitIndex);
            int remainder = index & (63);
            bits[bitIndex] |= (1L << remainder);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ClearBit(int index)
        {
            int bitIndex = index >>> 6;
            int remainder = index & (63);
            if (bits.Length > bitIndex)
            {
                bits[bitIndex] &= ~(1L << remainder);
            }
        }

        public void SetRange(int index, int count)
        {
            int start = index;
            int end = start + count;

            int startByteIndex = index >>> 6;
            int endByteIndex = end >>> 6;

            ResizeIfNeeded(endByteIndex);

            long mask = -1L >>> (64 - (start & 63)); //mask off bits in start long value
            bits[startByteIndex] |= (mask << ((end - 1) & 63)); //shift mask to correct for starting bit offset
            int byteLength = endByteIndex - startByteIndex;
            if (byteLength > 0) //start and end long values are not the same
            {
                long mask2 = -1L >>> (64 - (end & (63))); //mask off bits in end long value
                bits[endByteIndex] |= mask2;
                if (byteLength > 1) //fill middle between start end end long values
                {
                    Array.Fill(bits, -1, startByteIndex + 1, byteLength - 1);
                }
            }
        }

        public void ClearRange(int index, int count)
        {
            int start = index;
            int end = start + count;

            int startByteIndex = index >>> 6;
            int endByteIndex = end >>> 6;

            ResizeIfNeeded(endByteIndex);

            long mask = -1L >>> (64 - (start & 63)); //mask off bits in start long value
            bits[startByteIndex] &= ~(mask << ((end - 1) & 63)); //shift mask to correct for starting bit offset
            int byteLength = endByteIndex - startByteIndex;
            if (byteLength > 0) //start and end long values are not the same
            {
                long mask2 = -1L >>> (64 - (end & (63))); //mask off bits in end long value
                bits[endByteIndex] &= ~mask2;
                if (byteLength > 1) //fill middle between start end end long values
                {
                    Array.Fill(bits, 0, startByteIndex + 1, byteLength - 1);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void FlipBit(int index)
        {
            int bitIndex = index >>> 6;
            ResizeIfNeeded(bitIndex);
            int remainder = index & (63);
            bits[bitIndex] ^= (1L << remainder);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void OrBits(BitMask mask)
        {
            ResizeIfNeeded(mask.bits.Length);
            for (int i = 0; i < mask.bits.Length; i++)
            {
                bits[i] |= mask.bits[i];
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void OrFilteredBits(BitMask mask, BitMask filter)
        {
            int length = Math.Min(filter.bits.Length, mask.bits.Length);
            ResizeIfNeeded(length);

            for (int i = 0; i < length; i++)
            {
                bits[i] |= (mask.bits[i] & filter.bits[i]);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ClearBits(BitMask mask)
        {
            ResizeIfNeeded(mask.bits.Length);
            for (int i = 0; i < mask.bits.Length; i++)
            {
                bits[i] &= ~mask.bits[i];
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ClearMatchingBits(BitMask mask, BitMask filter)
        {
            int length = Math.Min(filter.bits.Length, mask.bits.Length);
            ResizeIfNeeded(length);
            for (int i = 0; i < length; i++)
            {
                bits[i] &= ~(mask.bits[i] & filter.bits[i]);
            }
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ClearAll()
        {
            Array.Clear(bits); //Fill with all 0s
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetAll()
        {
            Array.Fill(bits, -1); //Fill with all 1s
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void ResizeIfNeeded(int index)
        {
            if (bits.Length <= index)
            {
                Resize(index);
            }
        }

        void Resize(int index)
        {
            Array.Resize(ref bits, (int)BitOperations.RoundUpToPowerOf2((uint)index + 1));
        }

        /// <summary>
        /// Tests if all set bits of this ComponentMask match the other ComponentMask
        /// </summary>
        /// <param name="other"></param>
        /// <returns>true if all set bits of this ComponentMask match the other ComponentMask otherwise false</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool AllMatch(BitMask other)
        {
            if (other.bits.Length > bits.Length)
            {
                return false;
            }
            for (int i = 0; i < bits.Length; i++)
            {
                if ((bits[i] & other.bits[i]) != bits[i])
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Tests if any set bits of this ComponentMask match the other ComponentMask
        /// </summary>
        /// <param name="other"></param>
        /// <returns>true if any set bits of this ComponentMask match the other ComponentMask otherwise false</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool AnyMatch(BitMask other)
        {
            int length = Math.Min(bits.Length, other.bits.Length);
            for (int i = 0; i < length; i++)
            {
                if ((bits[i] & other.bits[i]) != 0)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Tests if all bits of this ComponentMask match the other ComponentMask
        /// </summary>
        /// <param name="other"></param>
        /// <returns>true if all bits of this ComponentMask match the other ComponentMask otherwise false</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool EqualMatch(BitMask other)
        {
            int length = Math.Min(bits.Length, other.bits.Length);
            return bits.AsSpan().SequenceEqual(other.bits);
        }

        /// <summary>
        /// Tests if all bits of this ComponentMask match the other ComponentMask
        /// </summary>
        /// <param name="other"></param>
        /// <returns>true if all bits of this ComponentMask match the other ComponentMask otherwise false</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool EqualMatchExact(BitMask other)
        {
            int length = Math.Min(bits.Length, other.bits.Length);
            for (int i = 0; i < length; i++)
            {
                if (bits[i] != other.bits[i])
                {
                    return false;
                }
            }
            for (int i = length; i < bits.Length; i++)
            {
                if (bits[i] != 0)
                {
                    return false;
                }
            }
            return true;
        }

        public override bool Equals(object? obj)
        {
            return obj is BitMask b && Equals(b);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                for (int i = 0; i < bits.Length; i++)
                {
                    hash = hash * 486187739 + (int)(bits[i] & 0xffffffff);
                    hash = hash * 486187739 + (int)(bits[i] << 32);
                }
                return hash;
            }
        }

        public bool Equals(BitMask? other)
        {
            if (other == null)
            {
                return false;
            }
            if (bits.Length != other.bits.Length)
            {
                return false;
            }
            return bits.AsSpan().SequenceEqual(other.bits);
        }

        public override string ToString()
        {
            if (bits.Length <= 0)
            {
                return "B: 0";
            }
            string agg = "B: " + Convert.ToString(bits[0], 2);
            for (int i = 1; i < bits.Length; i++)
            {
                agg = agg + Convert.ToString(bits[i], 2);
            }
            return agg;
        }
    }
}

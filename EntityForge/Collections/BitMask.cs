using CommunityToolkit.HighPerformance;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace EntityForge.Collections
{
    public sealed class BitMask : IEquatable<BitMask>
    {
        private ulong[] bits;

        public ReadOnlySpan<ulong> Bits
        {

            get
            {
                return bits;
            }
        }

        public BitMask()
        {
            bits = new ulong[1];
        }

        public BitMask(BitMask componentMask)
        {
            bits = [.. componentMask.bits];
        }

        public bool IsAllZeros()
        {
            return !Bits.ContainsAnyExcept(0ul);
        }

        public bool HasAnySet()
        {
            return Bits.ContainsAnyExcept(0ul);
        }

        public bool IsSet(int index)
        {
            int bitIndex = index >>> 6;
            if (bitIndex < bits.Length)
            {
                int remainder = index & (63);
                return (bits[bitIndex] & (1uL << remainder)) != 0;
            }
            return false;
        }

        public void SetBit(int index)
        {
            int bitIndex = index >>> 6;
            ResizeIfNeeded(bitIndex);
            int remainder = index & (63);
            bits[bitIndex] |= (1uL << remainder);
        }

        public void ClearBit(int index)
        {
            int bitIndex = index >>> 6;
            int remainder = index & (63);
            if (bits.Length > bitIndex)
            {
                bits[bitIndex] &= ~(1uL << remainder);
            }
        }

        public void SetRange(int index, int count)
        {
            int start = index;
            int end = start + count;

            int startByteIndex = index >>> 6;
            int endByteIndex = end >>> 6;

            ResizeIfNeeded(endByteIndex);

            ulong mask = ulong.MaxValue >>> (64 - (start & 63)); //mask off bits in start long value
            bits[startByteIndex] |= (mask << ((end - 1) & 63)); //shift mask to correct for starting bit offset
            int byteLength = endByteIndex - startByteIndex;
            if (byteLength > 0) //start and end long values are not the same
            {
                ulong mask2 = ulong.MaxValue >>> (64 - (end & (63))); //mask off bits in end long value
                bits[endByteIndex] |= mask2;
                if (byteLength > 1) //fill middle between start end end long values
                {
                    Array.Fill(bits, ulong.MaxValue, startByteIndex + 1, byteLength - 1);
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

            ulong mask = ulong.MaxValue >>> (64 - (start & 63)); //mask off bits in start long value
            bits[startByteIndex] &= ~(mask << ((end - 1) & 63)); //shift mask to correct for starting bit offset
            int byteLength = endByteIndex - startByteIndex;
            if (byteLength > 0) //start and end long values are not the same
            {
                ulong mask2 = ulong.MaxValue >>> (64 - (end & (63))); //mask off bits in end long value
                bits[endByteIndex] &= ~mask2;
                if (byteLength > 1) //fill middle between start end end long values
                {
                    Array.Fill(bits, 0uL, startByteIndex + 1, byteLength - 1);
                }
            }
        }


        public void FlipBit(int index)
        {
            int bitIndex = index >>> 6;
            ResizeIfNeeded(bitIndex);
            int remainder = index & (63);
            bits[bitIndex] ^= (1uL << remainder);
        }


        public void OrBits(BitMask mask)
        {
            ResizeIfNeeded(mask.bits.Length);
            for (int i = 0; i < mask.bits.Length; i++)
            {
                bits[i] |= mask.bits[i];
            }
        }

        public void XorBits(BitMask mask)
        {
            ResizeIfNeeded(mask.bits.Length);
            for (int i = 0; i < mask.bits.Length; i++)
            {
                bits[i] ^= mask.bits[i];
            }
        }

        public void OverrideUL(BitMask mask)
        {
            ResizeIfNeeded(mask.bits.Length);
            for (int i = 0; i < mask.bits.Length; i++)
            {
                bits[i] = mask.bits[i];
            }
        }

        public void OrFilteredBits(BitMask mask, BitMask filter)
        {
            int length = Math.Min(filter.bits.Length, mask.bits.Length);
            ResizeIfNeeded(length);

            for (int i = 0; i < length; i++)
            {
                bits[i] |= (mask.bits[i] & filter.bits[i]);
            }
        }


        public void ClearBits(BitMask mask)
        {
            ResizeIfNeeded(mask.bits.Length);
            for (int i = 0; i < mask.bits.Length; i++)
            {
                bits[i] &= ~mask.bits[i];
            }
        }


        public void ClearMatchingBits(BitMask mask, BitMask filter)
        {
            int length = Math.Min(filter.bits.Length, mask.bits.Length);
            ResizeIfNeeded(length);
            for (int i = 0; i < length; i++)
            {
                bits[i] &= ~(mask.bits[i] & filter.bits[i]);
            }
        }



        public void ClearAll()
        {
            Array.Clear(bits); //Fill with all 0s
        }


        public void SetAll()
        {
            Array.Fill(bits, ulong.MaxValue); //Fill with all 1s
        }


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
        public bool AllMatch(BitMask other)
        {
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
        /// Tests if all bits of the other ComponentMask are set in this ComponentMask
        /// </summary>
        /// <param name="other"></param>
        /// <returns>true if all bits of the other ComponentMask are set in this ComponentMask</returns>
        public bool AreSet(BitMask other)
        {
            int length = Math.Min(bits.Length, other.bits.Length);
            for (int i = length; i < other.bits.Length; i++)
            {
                if (other.bits[i] != 0)
                {
                    return false;
                }
            }
            for (int i = 0; i < length; i++)
            {
                if ((bits[i] & other.bits[i]) != other.bits[i])
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
        public bool EqualMatch(BitMask other)
        {
            return bits.AsSpan().SequenceEqual(other.bits);
        }

        /// <summary>
        /// Tests if all bits of this ComponentMask match the other ComponentMask
        /// </summary>
        /// <param name="other"></param>
        /// <returns>true if all bits of this ComponentMask match the other ComponentMask otherwise false</returns>
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
            unchecked
            {
                if (bits.Length <= 0)
                {
                    return "B: 0";
                }
                string agg = "B: " + Convert.ToString((long)bits[0], 2);
                for (int i = 1; i < bits.Length; i++)
                {
                    agg = agg + Convert.ToString((long)bits[i], 2);
                }
                return agg;
            }
        }
    }
}

using EntityForge.Helpers;
using CommunityToolkit.HighPerformance;
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsSet(int index)
        {
            int bitIndex = index / 64;
            ResizeIfNeeded(bitIndex);
            return (bits[bitIndex] &= 1u << index % 64) != 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetBit(int index)
        {
            int bitIndex = index / 64;
            ResizeIfNeeded(bitIndex);
            bits[bitIndex] |= 1u << index % 64;
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
        public void ClearFilteredBits(BitMask mask, BitMask filter)
        {
            int length = Math.Min(filter.bits.Length, mask.bits.Length);
            ResizeIfNeeded(length);
            for (int i = 0; i < length; i++)
            {
                bits[i] &= ~(mask.bits[i] & filter.bits[i]);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ClearBit(int index)
        {
            int bitIndex = index / 64;
            ResizeIfNeeded(bitIndex);
            bits[bitIndex] &= ~(1u << index % 64);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ClearAll()
        {
            Array.Clear(bits);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetAll()
        {
            Array.Fill(bits, unchecked((long)ulong.MaxValue));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void ResizeIfNeeded(int index)
        {
            bits = bits.EnsureContains(index);
        }

        /// <summary>
        /// Tests if all set bits of this ComponentMask match the other ComponentMask
        /// </summary>
        /// <param name="other"></param>
        /// <returns>true if all set bits of this ComponentMask match the other ComponentMask otherwise false</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool AllMatch(BitMask other)
        {
            int length = Math.Min(bits.Length, other.bits.Length);
            for (int i = 0; i < length; i++)
            {
                if ((bits[i] & other.bits[i]) != bits[i])
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Tests if all set bits of this ComponentMask match the other ComponentMask
        /// </summary>
        /// <param name="other"></param>
        /// <returns>true if all set bits of this ComponentMask match the other ComponentMask otherwise false</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool AllMatchExact(BitMask other)
        {
            int length = Math.Min(bits.Length, other.bits.Length);
            for (int i = 0; i < length; i++)
            {
                if ((bits[i] & other.bits[i]) != bits[i])
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
            for (int i = 0; i < length; i++)
            {
                if (bits[i] != other.bits[i])
                {
                    return false;
                }
            }
            return true;
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<long> GetSpan()
        {
            return bits.AsSpan();
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
            bool potential = bits.Length == other.bits.Length;
            if (!potential)
            {
                return false;
            }
            for (int i = 0; i < bits.Length; i++)
            {
                potential &= bits[i] == other.bits[i];
            }
            return potential;
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

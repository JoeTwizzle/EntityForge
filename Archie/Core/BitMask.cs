using CommunityToolkit.HighPerformance;
using System.Runtime.CompilerServices;

namespace Archie
{
    public class BitMask
    {
        private long[] bits;
        public BitMask()
        {
            bits = new long[1];
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public bool IsSet(int index)
        {
            int bitIndex = index / 64;
            ResizeIfNeeded(bitIndex);
            return (bits[bitIndex] &= 1u << (index % 64)) != 0;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public void SetBit(int index)
        {
            int bitIndex = index / 64;
            ResizeIfNeeded(bitIndex);
            bits[bitIndex] |= 1u << (index % 64);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public void ClearBit(int index)
        {
            int bitIndex = index / 64;
            ResizeIfNeeded(bitIndex);
            bits[bitIndex] &= ~(1u << (index % 64));
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public void ClearAll()
        {
            Array.Clear(bits);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public void SetAll()
        {
            Array.Fill(bits, unchecked((long)ulong.MaxValue));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void ResizeIfNeeded(int index)
        {
            int length = bits.Length;
            while (index >= length)
            {
                length *= 2;
            }
            Array.Resize(ref bits, length);
        }

        /// <summary>
        /// Tests if all set bits of this BitMask match the other BitMask
        /// </summary>
        /// <param name="other"></param>
        /// <returns>true if all set bits of this BitMask match the other BitMask otherwise false</returns>
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
        /// Tests if all set bits of this BitMask match the other BitMask
        /// </summary>
        /// <param name="other"></param>
        /// <returns>true if all set bits of this BitMask match the other BitMask otherwise false</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
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
        /// Tests if any set bits of this BitMask match the other BitMask
        /// </summary>
        /// <param name="other"></param>
        /// <returns>true if any set bits of this BitMask match the other BitMask otherwise false</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
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
        /// Tests if all bits of this BitMask match the other BitMask
        /// </summary>
        /// <param name="other"></param>
        /// <returns>true if all bits of this BitMask match the other BitMask otherwise false</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
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
        /// Tests if all bits of this BitMask match the other BitMask
        /// </summary>
        /// <param name="other"></param>
        /// <returns>true if all bits of this BitMask match the other BitMask otherwise false</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
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
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public Span<long> GetSpan()
        {
            return bits.AsSpan();
        }
    }
}

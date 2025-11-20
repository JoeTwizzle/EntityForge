using System.Buffers;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace EntityForge.Helpers
{
    internal static class ArrayExtentions
    {
        internal static T[] GrowIfNeededPooled<T>(this T[] array, int filled, int added, bool clear = false)
        {
            int sum = filled + added;
            int length = array.Length;
            if (length < sum)
            {
                //Grow by 2x
                //Keep doubling Capacity if we grow by a large amount
                do
                {
                    length *= 2;

                } while (length < sum);

                if (length > Array.MaxLength) length = Array.MaxLength;
                var newPool = ArrayPool<T>.Shared.Rent(length);

                Array.Copy(array, 0, newPool, 0, filled);
                ArrayPool<T>.Shared.Return(array, clear);
                array = newPool;
            }
            return array;
        }
        
        internal static void FillHole<T>(this T[] array, int holeIndex, int filled)
        {
            Array.Copy(array, filled - 1, array, holeIndex, 1);
        }

        internal interface IPredicateMatcher<T>
        {
            public bool IsEmpty(ref T item);
        }

        internal struct DefaultPredicateMatcher<T> : IPredicateMatcher<T> where T : INumber<T>
        {
            public bool IsEmpty(ref T item)
            {
                return item == T.Zero;
            }
        }
        
        internal static T[] GrowIfNeeded<T>(this T[] array, int filled, int added)
        {
            uint sum = (uint)(filled + added);
            uint length = (uint)array.Length;
            if (length < sum)
            {
                Array.Resize(ref array, (int)BitOperations.RoundUpToPowerOf2(sum));
            }
            return array;
        }

        internal static T[] EnsureContains<T>(this T[] array, int minSize)
        {
            return EnsureCapacity(array, minSize + 1);
        }

        internal static T[] EnsureCapacity<T>(this T[] array, int minSize)
        {
            if (array.Length < minSize)
            {
                Array.Resize(ref array, (int)BitOperations.RoundUpToPowerOf2((uint)minSize));
            }
            return array;
        }

        
        internal static Array GrowIfNeeded(this Array array, Type elementType, uint filled, uint added)
        {
            uint sum = filled + added;
            uint length = (uint)array.Length;
            if (length < sum)
            {
                var old = array;
                array = Array.CreateInstance(elementType, BitOperations.RoundUpToPowerOf2(sum));
                //move existing EntitiesPool
                Array.Copy(old, 0, array, 0, filled);
            }
            return array;
        }
    }

    internal record struct BufferInfo(bool isEmpty, int length)
    {
        public static implicit operator (bool isEmpty, int length)(BufferInfo value)
        {
            return (value.isEmpty, value.length);
        }

        public static implicit operator BufferInfo((bool isEmpty, int length) value)
        {
            return new BufferInfo(value.isEmpty, value.length);
        }
    }
}

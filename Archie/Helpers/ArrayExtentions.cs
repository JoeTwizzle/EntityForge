using System.Buffers;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Archie.Helpers
{
    internal static class ArrayExtentions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining )]
        internal static T[] GrowIfNeededPooled<T>(this T[] array, int filled, int added, bool clear = false)
        {
            int sum = filled + added;
            int length = array.Length;
            if (length < sum)
            {
                //Grow by 2x
                //Keep doubling size if we grow by a large amount
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

        [MethodImpl(MethodImplOptions.AggressiveInlining )]
        internal static int FindHoles<T>(this Span<T> array, ref BufferInfo[] pooledArray) where T : INumber<T>
        {
            var matcher = new DefaultPredicateMatcher<T>();
            return FindHoles(array, ref pooledArray, matcher);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining )]
        internal static int FindHoles<T>(this Span<T> span, ref BufferInfo[] pooledArray, IPredicateMatcher<T> matcher)
        {
            var buffer = pooledArray.GrowIfNeededPooled(0, span.Length);
            int count = 0;
            int matches = 0;
            bool lastState = false;
            for (int i = 0; i < span.Length; i++)
            {
                var currentState = matcher.IsEmpty(ref span[i]);
                if (lastState != currentState)
                {
                    buffer[count++] = new BufferInfo(lastState, matches);
                    matches = 0;
                    lastState = currentState;
                }
                matches++;
            }
            return count;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining )]
        internal static T[] Compact<T>(this T[] array, Span<BufferInfo> holes)
        {
            int lengthTotal = 0;
            for (int i = 0; i < holes.Length; i++)
            {
                lengthTotal += holes[i].length;
            }
            if (lengthTotal == array.Length)
            {
                return array;
            }
            var dest = new T[lengthTotal];
            int srcIndex = 0;
            int destIndex = 0;
            for (int i = 0; i < holes.Length; i++)
            {
                if (holes[i].isEmpty && i < holes.Length - 1)
                {
                    srcIndex += holes[i].length;
                    Array.Copy(array, srcIndex, dest, destIndex, holes[i + 1].length);
                }
                else
                {
                    destIndex += holes[i].length;
                }
            }
            return dest;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining )]
        internal static T[] GrowIfNeeded<T>(this T[] array, int filled, int added)
        {
            int sum = filled + added;
            int length = array.Length;
            if (length < sum)
            {
                //Grow by 2x
                //Keep doubling size if we grow by a large amount
                do
                {
                    length *= 2;

                } while (length < sum);

                if (length > Array.MaxLength) length = Array.MaxLength;

                Array.Resize(ref array, length);
            }
            return array;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining )]
        internal static Array GrowIfNeeded(this Array array, Type elementType, uint filled, uint added)
        {
            uint sum = filled + added;
            uint length = (uint)array.Length;
            if (length < sum)
            {
                var old = array;
                //Grow by 2x
                uint newCapacity = length * 2;
                //Keep doubling size if we grow by a large amount
                while (newCapacity < sum)
                {
                    newCapacity *= 2u;
                }

                if (newCapacity > Array.MaxLength) newCapacity = (uint)Array.MaxLength;

                var newPool = Array.CreateInstance(elementType, newCapacity);
                array = newPool;
                //move existing entities
                Array.Copy(old, 0, newPool, 0, filled);
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

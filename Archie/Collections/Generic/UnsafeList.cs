using Archie.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Archie.Collections
{
    internal class UnsafeList<T> : IDisposable
    {
        int _length;
        int _count;

        public int Count => _count;

        public int Length => _length;

        ArrayOrPointer<T> array;

        public UnsafeList(ArrayOrPointer<T> array, int length)
        {
            this.array = array;
            _count = 0;
            _length = 1;
        }

        public UnsafeList(int initialLength = 0)
        {
            array = ArrayOrPointer<T>.Create(initialLength);
        }

        public Span<T> GetData() => MemoryMarshal.CreateSpan(ref array.GetFirst(), _length);

        public ref T Add(T item)
        {
            if (_length >= _count)
            {

                _count = (int)BitOperations.RoundUpToPowerOf2((uint)++_count);
                if (array.IsUnmanaged)
                {
                    unsafe
                    {
                        array.GrowToUnmanaged(_count);
                    }
                }
                else
                {
                    array.GrowToManaged(_count);
                }
            }
            ref T i = ref array.GetRefAt(_length++);
            i = item;
            return ref i;
        }

        public ref T GetOrAdd(int index)
        {
            if (index >= _count)
            {
                _count = (int)BitOperations.RoundUpToPowerOf2((uint)index + 1);
                if (array.IsUnmanaged)
                {
                    unsafe
                    {
                        array.GrowToUnmanaged(_count);
                    }
                }
                else
                {
                    array.GrowToManaged(_count);
                }
            }
            return ref array.GetRefAt(index);
        }

        public void RemoveAt(int index)
        {
            array.GetRefAt(index) = array.GetRefAt(--_length);
        }

        public void RemoveAtStable(int index)
        {
            int len = (--_length) - index;
            if (len > 0)
            {
                if (array.IsUnmanaged)
                {
                    unsafe
                    {
                        array.CopyToUnmanaged(index + 1, array.UnmanagedData, index, len * Unsafe.SizeOf<T>());
                    }
                }
                else
                {
                    array.CopyToManaged(index + 1, array.ManagedData!, index, len);
                }
            }
        }

        public void Dispose()
        {
            array.Dispose();
        }
    }
}

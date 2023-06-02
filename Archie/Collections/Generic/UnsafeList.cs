using Archie.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Archie.Collections
{
    internal class UnsafeList<T> : IDisposable
    {
        int _count;
        int _length;

        public int Length => _length;

        public int Count => _count;

        ArrayOrPointer<T> array;

        public UnsafeList(int initialLength = 0)
        {
            array = ArrayOrPointer<T>.Create(initialLength);
            _length = initialLength;
            _count = 0;
        }

        public Span<T> GetRawData() => MemoryMarshal.CreateSpan(ref array.GetFirst(), _length);

        public Span<T> GetData() => MemoryMarshal.CreateSpan(ref array.GetFirst(), _count);

        public ref T Add(T item)
        {
            if (_count >= _length)
            {

                _length = (int)BitOperations.RoundUpToPowerOf2((uint)++_length);
                if (array.IsUnmanaged)
                {
                    unsafe
                    {
                        array.GrowToUnmanaged(_length);
                    }
                }
                else
                {
                    array.GrowToManaged(_length);
                }
            }
            ref T i = ref array.GetRefAt(_count++);
            i = item;
            return ref i;
        }

        public ref T GetOrAdd(int index)
        {
            if (index >= _length)
            {
                _length = (int)BitOperations.RoundUpToPowerOf2((uint)index + 1);
                if (array.IsUnmanaged)
                {
                    unsafe
                    {
                        array.GrowToUnmanaged(_length);
                    }
                }
                else
                {
                    array.GrowToManaged(_length);
                }
            }
            if (index >= _count)
            {
                _count = index + 1;
            }
            return ref array.GetRefAt(index);
        }

        public void RemoveAt(int index)
        {
            array.GetRefAt(index) = array.GetRefAt(--_count);
        }

        public void RemoveAtStable(int index)
        {
            int len = (--_count) - index;
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

using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace EntityForge.Collections
{
    internal sealed class UnsafeList : IDisposable
    {
        int _length;
        int _count;

        public int Count => _count;

        public int Length => _length;

        ArrayOrPointer array;
        public UnsafeList(ArrayOrPointer array, int length)
        {
            this.array = array;
            _count = 0;
            _length = 1;
        }

        public UnsafeList(Type type, int count)
        {
            array = ArrayOrPointer.CreateManaged(count, type);
        }

        public UnsafeList(int elementSize, int count)
        {
            array = ArrayOrPointer.CreateUnmanaged(count, elementSize);
        }

        public static UnsafeList CreateForComponent<T>() where T : struct, IComponent<T>
        {
            int length = 1;
            return new UnsafeList(ArrayOrPointer.CreateForComponent<T>(length), length);
        }

        public Span<T> GetData<T>() => MemoryMarshal.CreateSpan(ref array.GetFirst<T>(), _length);
        public Span<T> GetWrittenData<T>() => MemoryMarshal.CreateSpan(ref array.GetFirst<T>(), _count);

        public ref T Add<T>(T item)
        {
            if (_length >= _count)
            {

                _count = (int)BitOperations.RoundUpToPowerOf2((uint)++_count);
                if (array.IsUnmanaged)
                {
                    unsafe
                    {
                        array.GrowToUnmanaged(_count, sizeof(T));
                    }
                }
                else
                {
                    array.GrowToManaged(_count, typeof(T));
                }
            }
            ref T i = ref array.GetRefAt<T>(_length++);
            i = item;
            return ref i;
        }

        public ref T Get<T>(int index)
        {
            if (index >= _count)
            {
                _count = (int)BitOperations.RoundUpToPowerOf2((uint)index + 1);
                if (array.IsUnmanaged)
                {
                    unsafe
                    {
                        array.GrowToUnmanaged(_count, sizeof(T));
                    }
                }
                else
                {
                    array.GrowToManaged(_count, typeof(T));
                }
            }
            return ref array.GetRefAt<T>(index);
        }

        public void Remove<T>(T item)
        {
            if (item == null)
            {
                return;
            }
            var span = GetWrittenData<T>();
            for (int i = 0; i < span.Length; i++)
            {
                if (item.Equals(span[i]))
                {
                    RemoveAt<T>(i);
                    return;
                }
            }
        }

        public T RemoveAt<T>(int index)
        {
            var target = array.GetRefAt<T>(index);
            array.GetRefAt<T>(index) = array.GetRefAt<T>(--_length);
            return target;
        }

        public void RemoveAtStable<T>(int index)
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

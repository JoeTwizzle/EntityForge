using System.Numerics;
using System.Runtime.InteropServices;

namespace EntityForge.Collections
{
    internal class UnsafeList : IDisposable
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

        public Span<T> GetData<T>() where T : struct => MemoryMarshal.CreateSpan(ref array.GetFirst<T>(), _length);

        public ref T Add<T>(T item) where T : struct
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

        public ref T Get<T>(int index) where T : struct
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

        public T RemoveAt<T>(int index) where T : struct
        {
            var target = array.GetRefAt<T>(index);
            array.GetRefAt<T>(index) = array.GetRefAt<T>(--_length);
            return target;
        }

        public void Dispose()
        {
            array.Dispose();
        }
    }
}

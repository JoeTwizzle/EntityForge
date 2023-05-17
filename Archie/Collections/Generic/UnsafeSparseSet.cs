using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Archie.Collections.Generic
{
    internal sealed class UnsafeSparseSet<T> : IDisposable
    {
        private int _sparseLength;
        private int _denseLength;
        private int _denseCount;

        public int SparseLength => _sparseLength;

        public int DenseCount => _denseCount;

        private readonly ArrayOrPointer<T> denseArray;
        private readonly ArrayOrPointer<int> reverseSparseArray;
        private readonly ArrayOrPointer<int> sparseArray;

        public UnsafeSparseSet()
        {
            unsafe
            {
                if (!RuntimeHelpers.IsReferenceOrContainsReferences<T>())
                {
                    denseArray = new ArrayOrPointer<T>(NativeMemory.Alloc(Archetype.DefaultPoolSize, (nuint)sizeof(T)));
                }
                else
                {
                    denseArray = new ArrayOrPointer<T>(Array.CreateInstance(typeof(T), Archetype.DefaultPoolSize));
                }
                reverseSparseArray = new ArrayOrPointer<int>(NativeMemory.Alloc(Archetype.DefaultPoolSize, sizeof(int)));
                sparseArray = new ArrayOrPointer<int>(NativeMemory.Alloc(Archetype.DefaultPoolSize, sizeof(int)));
            }
        }

        public Span<int> GetSparseData() => MemoryMarshal.CreateSpan(ref sparseArray.GetFirst(), _sparseLength);

        public Span<T> GetDenseData() => MemoryMarshal.CreateSpan(ref denseArray.GetRefAt(1), _denseCount - 1);

        public Span<int> GetIndexData() => MemoryMarshal.CreateSpan(ref reverseSparseArray.GetRefAt(1), _denseCount - 1);

        public unsafe void CopyToUnmanaged(int index, void* dest, int destIndex, int sizeInBytes)
        {
            unsafe
            {
                denseArray.CopyToUnmanaged(sparseArray.GetValueAt(index), dest, destIndex, sizeInBytes);
            }
        }

        public void CopyToManaged(int index, Array dest, int destIndex, int length)
        {
            unsafe
            {
                denseArray.CopyToManaged(sparseArray.GetValueAt(index), dest, destIndex, length);
            }
        }

        public unsafe void CopyToUnmanagedRaw(int index, void* dest, int destIndex, int sizeInBytes)
        {
            unsafe
            {
                denseArray.CopyToUnmanaged(index, dest, destIndex, sizeInBytes);
            }
        }

        public void CopyToManagedRaw(int index, Array dest, int destIndex, int length)
        {
            unsafe
            {
                denseArray.CopyToManaged(index, dest, destIndex, length);
            }
        }

        public ref T Add(int index)
        {
            if (index >= _sparseLength)
            {
                _sparseLength = (int)BitOperations.RoundUpToPowerOf2((uint)index + 1);
                sparseArray.GrowToUnmanaged(_sparseLength);
            }
            ref int denseIndex = ref sparseArray.GetRefAt(index);
            denseIndex = ++_denseCount;
            if (denseIndex >= _denseLength)
            {
                _denseLength = (int)BitOperations.RoundUpToPowerOf2((uint)_denseCount);
                denseArray.GrowToUnmanaged(_denseLength);
                reverseSparseArray.GrowToUnmanaged(_denseLength);
            }
            reverseSparseArray.GetRefAt(DenseCount) = index;
            ref T result = ref denseArray.GetRefAt(DenseCount);
            result = default!;
            return ref result;
        }

        public ref T Add(int index, T value)
        {
            if (index >= _sparseLength)
            {
                _sparseLength = (int)BitOperations.RoundUpToPowerOf2((uint)index + 1);
                sparseArray.GrowToUnmanaged(_sparseLength);
            }
            ref int denseIndex = ref sparseArray.GetRefAt(index);
            denseIndex = ++_denseCount;
            if (denseIndex >= _denseLength)
            {
                _denseLength = (int)BitOperations.RoundUpToPowerOf2((uint)_denseCount);
                sparseArray.GrowToUnmanaged(_denseLength);
                reverseSparseArray.GrowToUnmanaged(_denseLength);
            }
            reverseSparseArray.GetRefAt(DenseCount) = index;
            ref var result = ref denseArray.GetRefAt(DenseCount);
            result = value;
            return ref result;
        }

        public bool Has(int index)
        {
            return (uint)index < _denseCount && sparseArray.GetRefAt(index) > 0;
        }

        public void RemoveAt(int index)
        {
            var valueIndex = sparseArray.GetRefAt(index);
            sparseArray.GetRefAt(index) = 0;
            for (int i = 0; i < _sparseLength; i++)
            {
                if (sparseArray.GetValueAt(i) == _denseCount)
                {
                    sparseArray.GetRefAt(i) = valueIndex;
                    denseArray.GetRefAt(valueIndex) = denseArray.GetRefAt(_denseCount);
                    denseArray.GetRefAt(_denseCount) = default!;
                    reverseSparseArray.GetRefAt(valueIndex) = reverseSparseArray.GetRefAt(_denseCount);
                    reverseSparseArray.GetRefAt(_denseCount) = default;
                    _denseCount--;
                    break;
                }
            }
        }

        public ref T Get(int index)
        {
            return ref denseArray.GetRefAt(sparseArray.GetRefAt(index));
        }

        public ref T GetOrAdd(int index)
        {
            if (Has(index))
            {
                return ref Get(index);
            }
            return ref Add(index);
        }

        public void Dispose()
        {
            reverseSparseArray.Dispose();
            sparseArray.Dispose();
            denseArray.Dispose();
        }
    }
}
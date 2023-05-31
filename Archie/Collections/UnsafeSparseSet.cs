using Archie.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Archie.Collections
{
    internal sealed class UnsafeSparseSet : IDisposable
    {
        private int _sparseLength;
        private int _denseLength;
        private int _denseCount;

        public int SparseLength => _sparseLength;

        public int DenseCount => _denseCount;

        private readonly ArrayOrPointer denseArray;
        private readonly ArrayOrPointer<int> reverseSparseArray;
        private readonly ArrayOrPointer<int> sparseArray;

        public UnsafeSparseSet(Type type)
        {
            unsafe
            {

                denseArray = ArrayOrPointer.CreateManaged(Archetype.DefaultPoolSize, type);
                reverseSparseArray = new ArrayOrPointer<int>(NativeMemory.Alloc(Archetype.DefaultPoolSize, sizeof(int)));
                sparseArray = new ArrayOrPointer<int>(NativeMemory.Alloc(Archetype.DefaultPoolSize, sizeof(int)));
            }
        }

        public UnsafeSparseSet(int sizeInBytes)
        {
            unsafe
            {
                denseArray = ArrayOrPointer.CreateUnmanaged(Archetype.DefaultPoolSize, sizeInBytes);
                reverseSparseArray = new ArrayOrPointer<int>(NativeMemory.Alloc(Archetype.DefaultPoolSize, sizeof(int)));
                sparseArray = new ArrayOrPointer<int>(NativeMemory.Alloc(Archetype.DefaultPoolSize, sizeof(int)));
            }
        }

        public Span<int> GetSparseData() => MemoryMarshal.CreateSpan(ref sparseArray.GetFirst(), _sparseLength);

        public Span<T> GetDenseData<T>() where T : struct => MemoryMarshal.CreateSpan(ref denseArray.GetRefAt<T>(1), _denseCount - 1);

        public Span<int> GetIndexData() => MemoryMarshal.CreateSpan(ref reverseSparseArray.GetRefAt(1), _denseCount - 1);

        public ref T Add<T>(int index) where T : struct
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
                denseArray.GrowToUnmanaged(_denseLength, Unsafe.SizeOf<T>());
                reverseSparseArray.GrowToUnmanaged(_denseLength);
            }
            reverseSparseArray.GetRefAt(DenseCount) = index;
            ref var result = ref denseArray.GetRefAt<T>(DenseCount);
            result = default;
            return ref result;
        }

        public ref T Add<T>(int index, T value) where T : struct
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
            ref var result = ref denseArray.GetRefAt<T>(DenseCount);
            result = value;
            return ref result;
        }

        public bool Has(int index)
        {
            return (uint)index < _denseCount && sparseArray.GetRefAt(index) > 0;
        }

        public bool TryGetIndex(int index, out int denseIndex)
        {
            if ((uint)index < _denseCount)
            {
                denseIndex = sparseArray.GetRefAt(index);
                return denseIndex > 0;
            }
            denseIndex = 0;
            return false;
        }

        public void RemoveAt<T>(int index) where T : struct
        {
            var valueIndex = sparseArray.GetRefAt(index);
            sparseArray.GetRefAt(index) = 0;
            for (int i = 0; i < _sparseLength; i++)
            {
                if (sparseArray.GetValueAt(i) == _denseCount)
                {
                    sparseArray.GetRefAt(i) = valueIndex;
                    denseArray.GetRefAt<T>(valueIndex) = denseArray.GetRefAt<T>(_denseCount);
                    denseArray.GetRefAt<T>(_denseCount) = default;
                    reverseSparseArray.GetRefAt(valueIndex) = reverseSparseArray.GetRefAt(_denseCount);
                    reverseSparseArray.GetRefAt(_denseCount) = default;
                    _denseCount--;
                    break;
                }
            }
        }

        public void RemoveAt(int index, int size)
        {
            var valueIndex = sparseArray.GetRefAt(index);
            sparseArray.GetRefAt(index) = 0;
            for (int i = 0; i < _sparseLength; i++)
            {
                if (sparseArray.GetValueAt(i) == _denseCount)
                {
                    sparseArray.GetRefAt(i) = valueIndex;
                    Unsafe.CopyBlock(ref denseArray.GetRefAt(valueIndex, size), ref denseArray.GetRefAt(_denseCount, size), (uint)size);
                    Unsafe.InitBlock(ref denseArray.GetRefAt(_denseCount, size), 0, (uint)size);
                    reverseSparseArray.GetRefAt(valueIndex) = reverseSparseArray.GetRefAt(_denseCount);
                    reverseSparseArray.GetRefAt(_denseCount) = default;
                    _denseCount--;
                    break;
                }
            }
        }

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

        public ref T Get<T>(int index) where T : struct
        {
            return ref denseArray.GetRefAt<T>(sparseArray.GetValueAt(index));
        }

        public ref T GetOrAdd<T>(int index) where T : struct
        {
            if (Has(index))
            {
                return ref Get<T>(index);
            }
            return ref Add<T>(index);
        }

        public void Dispose()
        {
            reverseSparseArray.Dispose();
            sparseArray.Dispose();
            denseArray.Dispose();
        }
    }
}
using EntityForge.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace EntityForge.Collections
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
                _sparseLength = Archetype.DefaultPoolSize;
                _denseLength = Archetype.DefaultPoolSize;
                denseArray = ArrayOrPointer.CreateManaged(_denseLength, type);
                reverseSparseArray = ArrayOrPointer<int>.CreateUnmanaged(_denseLength);
                sparseArray = ArrayOrPointer<int>.CreateUnmanaged(_sparseLength);
                for (int i = 0; i < _sparseLength; i++)
                {
                    sparseArray.GetRefAt(i) = 0;
                }
            }
        }

        public UnsafeSparseSet(int sizeInBytes)
        {
            unsafe
            {
                _sparseLength = Archetype.DefaultPoolSize;
                _denseLength = Archetype.DefaultPoolSize;
                denseArray = ArrayOrPointer.CreateUnmanaged(_denseLength, sizeInBytes);
                reverseSparseArray = ArrayOrPointer<int>.CreateUnmanaged(_denseLength);
                sparseArray = ArrayOrPointer<int>.CreateUnmanaged(_sparseLength);
                for (int i = 0; i < _sparseLength; i++)
                {
                    sparseArray.GetRefAt(i) = 0;
                }
            }
        }

        public Span<int> GetSparseData() => MemoryMarshal.CreateSpan(ref sparseArray.GetFirst(), _sparseLength);

        public Span<T> GetDenseData<T>() where T : struct => MemoryMarshal.CreateSpan(ref denseArray.GetRefAt<T>(1), _denseCount - 1);

        public Span<int> GetIndexData() => MemoryMarshal.CreateSpan(ref reverseSparseArray.GetRefAt(1), _denseCount - 1);

        public ref T Add<T>(int index) where T : struct
        {
            return ref Add<T>(index, default);
        }

        public ref T Add<T>(int index, [AllowNull] T value) where T : struct
        {
            if (index >= _sparseLength)
            {
                int prevLength = _sparseLength;
                _sparseLength = (int)BitOperations.RoundUpToPowerOf2((uint)index + 1);
                sparseArray.GrowTo(_sparseLength);
                MemoryMarshal.CreateSpan(ref sparseArray.GetRefAt(prevLength), _sparseLength - prevLength).Clear();
            }
            ref int denseIndex = ref sparseArray.GetRefAt(index);
            denseIndex = ++_denseCount;
            if (denseIndex >= _denseLength)
            {
                _denseLength = (int)BitOperations.RoundUpToPowerOf2((uint)(_denseCount + 1));
                if (denseArray.IsUnmanaged)
                {
                    denseArray.GrowToUnmanaged(_denseLength, Unsafe.SizeOf<T>());
                }
                else
                {
                    denseArray.GrowToManaged(_denseLength, typeof(T));
                }
                reverseSparseArray.GrowTo(_denseLength);
            }
            reverseSparseArray.GetRefAt(denseIndex) = index;
            ref var result = ref denseArray.GetRefAt<T>(denseIndex);
            result = value;
            return ref result;
        }

        public bool Has(int index)
        {
            return (uint)index < _sparseLength && sparseArray.GetValueAt(index) > 0;
        }

        public bool TryGetIndex(int index, out int denseIndex)
        {
            if ((uint)index < _sparseLength)
            {
                denseIndex = sparseArray.GetValueAt(index);
                return denseIndex > 0;
            }
            denseIndex = 0;
            return false;
        }

        public void RemoveAt<T>(int index) where T : struct
        {
            if (_denseCount > 0)
            {
                var oldDenseIndex = sparseArray.GetRefAt(index);
                sparseArray.GetRefAt(index) = 0;
                int lastSparseIndex = reverseSparseArray.GetRefAt(_denseCount);
                sparseArray.GetRefAt(lastSparseIndex) = oldDenseIndex;
                denseArray.GetRefAt<T>(oldDenseIndex) = denseArray.GetRefAt<T>(_denseCount);
                denseArray.GetRefAt<T>(_denseCount) = default;
                reverseSparseArray.GetRefAt(oldDenseIndex) = lastSparseIndex;
                reverseSparseArray.GetRefAt(_denseCount) = default;
                _denseCount--;
            }
        }

        public void RemoveAt(int index, int size)
        {
            if (_denseCount > 0)
            {
                var oldDenseIndex = sparseArray.GetRefAt(index);
                sparseArray.GetRefAt(index) = 0;
                int lastSparseIndex = reverseSparseArray.GetRefAt(_denseCount);
                sparseArray.GetRefAt(lastSparseIndex) = oldDenseIndex;
                Unsafe.CopyBlock(ref denseArray.GetRefAt(oldDenseIndex, size), ref denseArray.GetRefAt(_denseCount, size), (uint)size);
                Unsafe.InitBlock(ref denseArray.GetRefAt(_denseCount, size), 0, (uint)size);
                reverseSparseArray.GetRefAt(oldDenseIndex) = lastSparseIndex;
                reverseSparseArray.GetRefAt(_denseCount) = default;
                _denseCount--;
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

        public void ClearManaged()
        {
            var data = denseArray.ManagedData;
            if (data != null)
            {
                Array.Clear(data);
            }
        }

        public void Dispose()
        {
            reverseSparseArray.Dispose();
            sparseArray.Dispose();
            denseArray.Dispose();
        }
    }
}
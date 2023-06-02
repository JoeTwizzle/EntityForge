using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Archie.Collections.Generic
{
    internal unsafe sealed class ArrayOrPointer<T> : IEquatable<ArrayOrPointer<T>>, IDisposable
    {
        public bool IsUnmanaged
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            [MemberNotNullWhen(false, nameof(ManagedData))]
            get
            {
                return ManagedData is null;
            }
        }

        public T[]? ManagedData;
        public void* UnmanagedData;

#pragma warning disable CA1000 // Do not declare static members on generic types
        public static ArrayOrPointer<T> CreateUnmanaged(int count)
        {
            return new ArrayOrPointer<T>(NativeMemory.AlignedAlloc((nuint)(count * sizeof(T)), 32));
        }

        public static ArrayOrPointer<T> CreateManaged(int count)
        {
            return new ArrayOrPointer<T>(new T[count]);
        }

        public static ArrayOrPointer<T> Create(int count)
        {
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                return new ArrayOrPointer<T>(new T[count]);
            }
            else
            {
                return new ArrayOrPointer<T>(NativeMemory.AlignedAlloc((nuint)(count * sizeof(T)), 32));
            }
        }
#pragma warning restore CA1000 // Do not declare static members on generic types

        public ArrayOrPointer(T[] buffer)
        {
            ManagedData = buffer;
        }

        public ArrayOrPointer(void* unmanagedBuffer)
        {
            UnmanagedData = unmanagedBuffer;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void GrowToUnmanaged(int elementCount)
        {
            UnmanagedData = NativeMemory.AlignedRealloc(UnmanagedData, (nuint)(elementCount * sizeof(T)), 32);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void GrowToManaged(int elementCount)
        {
            Array.Resize(ref ManagedData!, elementCount);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void GrowTo(int elementCount)
        {
            if (IsUnmanaged)
            {
                GrowToUnmanaged(elementCount);
            }
            else
            {
                GrowToManaged(elementCount);
            }
        }

        public ref T GetFirst()
        {
            if (IsUnmanaged)
            {
                return ref Unsafe.AsRef<T>(UnmanagedData);
            }
            else
            {
                return ref MemoryMarshal.GetArrayDataReference((T[])ManagedData);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T GetRefAt(int index)
        {
            if (IsUnmanaged)
            {
                return ref ((T*)UnmanagedData)[index];
            }
            else
            {
                return ref ((T[])ManagedData)[index];
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T GetValueAt(int index)
        {
            if (IsUnmanaged)
            {
                return ((T*)UnmanagedData)[index];
            }
            else
            {
                return ((T[])ManagedData)[index];
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void FillHoleManaged(int index, int last)
        {
            Array.Copy(ManagedData!, last, ManagedData!, index, 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void FillHoleUnmanaged(int index, int last)
        {
            var ptr = (byte*)UnmanagedData;
            NativeMemory.Copy(ptr + last, ptr + index, (nuint)sizeof(T));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyToManaged(int srcIndex, Array dest, int destIndex, int length)
        {
            Array.Copy(ManagedData!, srcIndex, dest!, destIndex, length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void CopyToUnmanaged(int srcIndex, void* dest, int destIndex, int sizeInBytes)
        {
            var ptr = (byte*)UnmanagedData;
            Buffer.MemoryCopy(ptr + srcIndex * sizeInBytes, ptr + destIndex * sizeInBytes, sizeInBytes, sizeInBytes);
        }

        public override bool Equals(object? obj)
        {
            return obj is ArrayOrPointer<T> p && Equals(p);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 486187739 + (IsUnmanaged ? 1 : 0);
                hash = hash * 486187739 + (IsUnmanaged ? (int)UnmanagedData : ManagedData!.GetHashCode());
                return hash;
            }
        }

        public static bool operator ==(ArrayOrPointer<T> left, ArrayOrPointer<T> right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ArrayOrPointer<T> left, ArrayOrPointer<T> right)
        {
            return !(left == right);
        }

        public bool Equals(ArrayOrPointer<T>? other)
        {
            if (other is null)
            {
                return false;
            }
            return IsUnmanaged ? UnmanagedData == other.UnmanagedData  : ManagedData == other.ManagedData;
        }

        public Span<T> GetSpan(int length)
        {
            if (IsUnmanaged)
            {
                return new Span<T>(UnmanagedData, length);
            }
            else
            {
                return new Span<T>(ManagedData, 0, length);
            }
        }

        public void Dispose()
        {
            if (IsUnmanaged)
            {
                NativeMemory.AlignedFree(UnmanagedData);
            }
            else
            {
                ManagedData = null;
            }
        }
    }
}
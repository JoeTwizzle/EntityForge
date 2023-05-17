using CommunityToolkit.HighPerformance.Buffers;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace Archie.Collections
{
    public unsafe struct ArrayOrPointer : IEquatable<ArrayOrPointer>, IDisposable
    {
        public bool IsUnmanaged
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            [MemberNotNullWhen(false, nameof(ManagedData))]
            get
            {
                return ManagedData == null;
            }
        }

        public Array? ManagedData;
        public void* UnmanagedData;

        public static ArrayOrPointer CreateUnmanaged(int count, int sizeInBytes)
        {
            return new ArrayOrPointer(NativeMemory.AlignedAlloc((nuint)(count * sizeInBytes), 32));
        }

        public static ArrayOrPointer CreateManaged(int count, Type type)
        {
            return new ArrayOrPointer(Array.CreateInstance(type, count));
        }

        public static ArrayOrPointer CreateForComponent<T>(int length = Archetype.DefaultPoolSize) where T : struct, IComponent<T>
        {
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                return CreateManaged(length, typeof(T));
            }
            else
            {
                return CreateUnmanaged(length, sizeof(T));
            }
        }

        private ArrayOrPointer(Array buffer)
        {
            ManagedData = buffer;
        }

        private ArrayOrPointer(void* unmanagedBuffer)
        {
            UnmanagedData = unmanagedBuffer;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void GrowToUnmanaged(int elementCount, int elementSize)
        {
            UnmanagedData = NativeMemory.AlignedRealloc(UnmanagedData, (nuint)(elementCount * elementSize), 32);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void GrowToManaged(int elementCount, Type arrayType)
        {
            var oldData = ManagedData;
            var newPool = Array.CreateInstance(arrayType, elementCount);
            ManagedData = newPool;
            //move existing EntitiesPool
            Array.Copy(oldData!, 0, newPool, 0, oldData!.Length);
        }

        public ref T GetFirst<T>() where T : struct
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
        public ref T GetRefAt<T>(int index) where T : struct
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
        public T GetValueAt<T>(int index) where T : struct
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
        public void FillHoleUnmanaged(int index, int last, int sizeInBytes)
        {
            var ptr = (byte*)UnmanagedData;
            Buffer.MemoryCopy(ptr + last, ptr + index, sizeInBytes, sizeInBytes);
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
            return obj is ArrayOrPointer p && Equals(p);
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

        public static bool operator ==(ArrayOrPointer left, ArrayOrPointer right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ArrayOrPointer left, ArrayOrPointer right)
        {
            return !(left == right);
        }

        public bool Equals(ArrayOrPointer other)
        {
            return IsUnmanaged ? UnmanagedData == other.UnmanagedData : ManagedData == other.ManagedData;
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
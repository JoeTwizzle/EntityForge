using CommunityToolkit.HighPerformance.Buffers;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace Archie
{
    public unsafe struct ArrayOrPointer : IEquatable<ArrayOrPointer>
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

        public ArrayOrPointer(Array? buffer)
        {
            ManagedData = buffer;
        }

        public ArrayOrPointer(void* unmanagedBuffer)
        {
            UnmanagedData = unmanagedBuffer;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void GrowToUnmanaged(int elementCount, int elementSize)
        {
            UnmanagedData = NativeMemory.Realloc(UnmanagedData, (nuint)(elementCount * elementSize));
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T GetRefAt<T>(int index) where T : struct, IComponent<T>
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
            Buffer.MemoryCopy(ptr + srcIndex, ptr + destIndex, sizeInBytes, sizeInBytes);
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
    }
}
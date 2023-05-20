using CommunityToolkit.HighPerformance.Buffers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace Archie.Collections.Generic
{
    public unsafe struct ArrayOrPointer<T> : IEquatable<ArrayOrPointer<T>>, IDisposable
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

        public T[]? ManagedData;
        public void* UnmanagedData;
        public int length;

#pragma warning disable CA1000 // Do not declare static members on generic types
        public static ArrayOrPointer<T> CreateUnmanaged(int count)
        {
            var a = new ArrayOrPointer<T>(NativeMemory.Alloc((nuint)(count * sizeof(T))));
            a.length = count;
            return a;
        }

        public static ArrayOrPointer<T> CreateManaged(int count)
        {
            var a = new ArrayOrPointer<T>(new T[count]);
            a.length = count;
            return a;
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
            length = elementCount;
            UnmanagedData = NativeMemory.Realloc(UnmanagedData, (nuint)(elementCount * Unsafe.SizeOf<T>()));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void GrowToManaged(int elementCount)
        {
            length = elementCount;
            var oldData = ManagedData;
            ManagedData = new T[elementCount];
            //move existing EntitiesPool
            Array.Copy(oldData!, 0, ManagedData, 0, oldData!.Length);
        }

        public ref T GetFirst()
        {
            Debug.Assert(length > 0);
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
            Debug.Assert(index < length);
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
            Debug.Assert(index < length);
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
            Debug.Assert(index < length);
            Debug.Assert(last < length);
            Array.Copy(ManagedData!, last, ManagedData!, index, 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void FillHoleUnmanaged(int index, int last)
        {
            Debug.Assert(index < length);
            Debug.Assert(last < length);
            var ptr = (byte*)UnmanagedData;
            NativeMemory.Copy(ptr + last, ptr + index, (nuint)sizeof(T));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyToManaged(int srcIndex, Array dest, int destIndex, int length)
        {
            Debug.Assert(srcIndex + length <= this.length);
            Debug.Assert(srcIndex < this.length);
            Array.Copy(ManagedData!, srcIndex, dest!, destIndex, length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void CopyToUnmanaged(int srcIndex, void* dest, int destIndex, int sizeInBytes)
        {
            Debug.Assert(srcIndex + sizeInBytes / sizeof(T) <= this.length);
            Debug.Assert(srcIndex < this.length);
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

        public bool Equals(ArrayOrPointer<T> other)
        {
            return IsUnmanaged ? UnmanagedData == other.UnmanagedData : ManagedData == other.ManagedData;
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
                NativeMemory.Free(UnmanagedData);
            }
            else
            {
                ManagedData = null;
            }
        }
    }
}
using Archie.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Archie
{
    public readonly struct ComponentView<T> : IEquatable<ComponentView<T>> where T : struct
    {
        public readonly Span<T> Span => new Span<T>(array, 0, length);
        private readonly T[] array;
        private readonly int length;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ComponentView(T[] array, int length)
        {
            this.length = length;
            this.array = array;
        }

        //public ref T this[int index]
        //{
        //    [MethodImpl(MethodImplOptions.AggressiveInlining)]
        //    get
        //    {
        //        if ((uint)index >= (uint)length)
        //        {
        //            ThrowHelper.ThrowArgumentOutOfRangeException();
        //        }
        //        return ref array[index];
        //    }
        //}

        public override bool Equals(object? obj)
        {
            return obj is ComponentView<T> c && Equals(c);
        }

        public override int GetHashCode()
        {
            int hash = 17;
            hash = hash * 486187739 + array.GetHashCode();
            hash = hash * 486187739 + length;
            return hash;
        }

        public static bool operator ==(ComponentView<T> left, ComponentView<T> right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ComponentView<T> left, ComponentView<T> right)
        {
            return !(left == right);
        }

        public bool Equals(ComponentView<T> other)
        {
            return other.array == this.array && other.length == this.length;
        }
    }

    public readonly struct ReadonlyComponentView<T> : IEquatable<ReadonlyComponentView<T>> where T : struct
    {
        public readonly ReadOnlySpan<T> Span => new ReadOnlySpan<T>(array, 0, length);
        private readonly T[] array;
        public readonly int length;

        public ReadonlyComponentView(T[] array, int length)
        {
            this.length = length;
            this.array = array;
        }

        //public ref readonly T this[int index]
        //{
        //    [MethodImpl(MethodImplOptions.AggressiveInlining)]
        //    get
        //    {
        //        if ((uint)index >= (uint)length)
        //        {
        //            ThrowHelper.ThrowArgumentOutOfRangeException();
        //        }

        //        return ref array[index];
        //    }
        //}
        public override bool Equals(object? obj)
        {
            return obj is ReadonlyComponentView<T> c && Equals(c);
        }

        public override int GetHashCode()
        {
            int hash = 17;
            hash = hash * 486187739 + array.GetHashCode();
            hash = hash * 486187739 + length;
            return hash;
        }

        public static bool operator ==(ReadonlyComponentView<T> left, ReadonlyComponentView<T> right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ReadonlyComponentView<T> left, ReadonlyComponentView<T> right)
        {
            return !(left == right);
        }

        public bool Equals(ReadonlyComponentView<T> other)
        {
            return other.array == this.array && other.length == this.length;
        }
    }
}

using Archie.Collections;
using Archie.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Archie
{
    public struct ComponentPoolSegment<T> : IEquatable<ComponentPoolSegment<T>> where T : struct, IComponent<T>
    {
        public readonly ArrayOrPointer Pool;
        public readonly int Length;

        public ComponentPoolSegment(ArrayOrPointer data, int length)
        {
            Pool = data;
            Length = length;
        }

        public ref T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if ((uint)index > Length)
                {
                    ThrowHelper.ThrowArgumentOutOfRange_IndexException();
                }
                return ref Pool.GetRefAt<T>(index);
            }
        }

        public override bool Equals(object? obj)
        {
            return obj is ComponentPoolSegment<T> p && Equals(p);
        }

        public override int GetHashCode()
        {
            int hash = 17;
            hash = hash * 486187739 + Pool.GetHashCode();
            hash = hash * 486187739 + Length.GetHashCode();
            return hash;
        }

        public static bool operator ==(ComponentPoolSegment<T> left, ComponentPoolSegment<T> right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ComponentPoolSegment<T> left, ComponentPoolSegment<T> right)
        {
            return !(left == right);
        }

        public bool Equals(ComponentPoolSegment<T> other)
        {
            return Length == other.Length && Pool.Equals(other.Pool);
        }
    }
}

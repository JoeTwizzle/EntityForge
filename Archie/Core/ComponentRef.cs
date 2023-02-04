using CommunityToolkit.HighPerformance.Helpers;
using CommunityToolkit.HighPerformance;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Archie
{
    /// <summary>
    /// Represents a refrence to component.
    /// Only valid for the duration of a Query.
    /// DO NOT STORE ANY LONGER!
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public readonly struct ComponentRef<T> : IEquatable<ComponentRef<T>>
    {
        readonly T[] buffer;
        readonly int index;

        public ComponentRef(T[] buffer, int index)
        {
            this.buffer = buffer;
            this.index = index;
        }

        public override bool Equals(object? obj)
        {
            return obj is ComponentRef<T> r && Equals(r);
        }

        public override int GetHashCode()
        {
            int hash = 17;
            hash = hash * 486187739 + index;
            hash = hash * 486187739 + buffer.GetHashCode();
            return hash;
        }

        public static bool operator ==(ComponentRef<T> left, ComponentRef<T> right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ComponentRef<T> left, ComponentRef<T> right)
        {
            return !(left == right);
        }

        public bool Equals(ComponentRef<T> other)
        {
            return other.buffer == buffer && other.index == index;
        }

        /// <summary>
        /// Gets the <typeparamref name="T"/> reference represented by the current <see cref="Ref{T}"/> instance.
        /// </summary>
        public ref T Value
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref buffer[index];
        }
        /// <summary>
        /// Implicitly gets the <typeparamref name="T"/> value from a given <see cref="Ref{T}"/> instance.
        /// </summary>
        /// <param name="reference">The input <see cref="Ref{T}"/> instance.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2225:Operator overloads have named alternates", Justification = "Not needed")]
        public static implicit operator T(ComponentRef<T> reference)
        {
            return reference.Value;
        }
    }
}

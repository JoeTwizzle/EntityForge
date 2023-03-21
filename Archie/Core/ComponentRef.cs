using CommunityToolkit.HighPerformance;
using System.Runtime.CompilerServices;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Archie
{
    /// <summary>
    /// Represents a refrence to component.
    /// Only valid for the duration of a Query.
    /// DO NOT STORE ANY LONGER!
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public unsafe struct ComponentRef<T> : IEquatable<ComponentRef<T>>
    {
#pragma warning disable CS8500 // This takes the address of, gets the Capacity of, or declares a pointer to a managed type
        T* data;

        public ComponentRef(T* data)
        {
            this.data = data;
        }


        public override bool Equals(object? obj)
        {
            return obj is ComponentRef<T> r && Equals(r);
        }

        public override int GetHashCode()
        {
            int hash = 17;
            hash = hash * 486187739 + (int)(data);
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
            return (int)(other.data) == (int)(data);
        }

        /// <summary>
        /// Gets the <typeparamref name="T"/> reference represented by the current <see cref="Ref{T}"/> instance.
        /// </summary>
        public ref T Value
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref Unsafe.AsRef<T>((void*)data);
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

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        internal void Next()
        {
            data = (T*)Unsafe.Add<T>((void*)data, 1);
        }
#pragma warning restore CS8500 // This takes the address of, gets the Capacity of, or declares a pointer to a managed type
    }
}

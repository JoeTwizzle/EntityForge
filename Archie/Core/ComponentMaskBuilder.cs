using System.Buffers;
using System.Diagnostics.CodeAnalysis;

namespace Archie
{
#pragma warning disable CA1815 // Override equals and operator equals on value types
    public struct ComponentMaskBuilder
#pragma warning restore CA1815 // Override equals and operator equals on value types
    {
        internal Type[] Included;
        internal Type[] Excluded;
        int includedCount;
        int excludedCount;

        internal static ComponentMaskBuilder Create()
        {
            return new ComponentMaskBuilder(ArrayPool<Type>.Shared.Rent(8), ArrayPool<Type>.Shared.Rent(8));
        }

        internal ComponentMaskBuilder(Type[] included, Type[] excluded)
        {
            Included = included;
            Excluded = excluded;
        }

        [UnscopedRefAttribute]
        public ref ComponentMaskBuilder Inc<T>() where T : struct, IComponent<T>
        {
            Included[includedCount++] = typeof(T);
            ResizeIncIfNeeded();
            return ref this;
        }

        [UnscopedRefAttribute]
        public ref ComponentMaskBuilder Exc<T>() where T : struct, IComponent<T>
        {
            Excluded[excludedCount++] = typeof(T);
            ResizeExcIfNeeded();
            return ref this;
        }

        void ResizeIncIfNeeded()
        {
            if (Included.Length >= includedCount)
            {
                ArrayPool<Type>.Shared.Return(Included);
                Included = ArrayPool<Type>.Shared.Rent(includedCount * 2);
            }
        }

        void ResizeExcIfNeeded()
        {
            if (Excluded.Length >= excludedCount)
            {
                ArrayPool<Type>.Shared.Return(Excluded);
                Excluded = ArrayPool<Type>.Shared.Rent(excludedCount * 2);
            }
        }

        public ComponentMask End()
        {
            var inc = Included.AsSpan(0, includedCount).ToArray();
            var exc = Excluded.AsSpan(0, excludedCount).ToArray();
            ArrayPool<Type>.Shared.Return(Included);
            ArrayPool<Type>.Shared.Return(Excluded);
            return new ComponentMask(inc, exc);
        }
    }
}

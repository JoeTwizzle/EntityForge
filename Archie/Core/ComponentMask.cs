using CommunityToolkit.HighPerformance.Helpers;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Archie
{
    public sealed class ComponentMask : IEquatable<ComponentMask>
    {
        public struct ComponentMaskBuilder
        {
            internal Type[] Included;
            internal Type[] Excluded;
            int includedCount;
            int excludedCount;

            public static ComponentMaskBuilder Create()
            {
                return new ComponentMaskBuilder(ArrayPool<Type>.Shared.Rent(8), ArrayPool<Type>.Shared.Rent(8));
            }

            public ComponentMaskBuilder(Type[] included, Type[] excluded)
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

        public readonly Type[] Included;
        public readonly Type[] Excluded;
        readonly int hashCode;

        public ComponentMask(Type[] included, Type[] excluded)
        {
            Included = included;
            Excluded = excluded;
            hashCode = World.GetComponentMaskHash(this);
        }

        public static ComponentMaskBuilder Create()
        {
            return ComponentMaskBuilder.Create();
        }

        public override bool Equals(object? obj)
        {
            return obj is ComponentMask && Equals((ComponentMask)obj);
        }

        public bool Equals(ComponentMask? other)
        {
            return other != null && hashCode == other.hashCode;
        }

        public override int GetHashCode()
        {
            return hashCode;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Archie
{
    public class ComponentMask : IEquatable<ComponentMask>
    {
        public readonly BitMask IncludeMask;
        public readonly BitMask ExcludeMask;

        public ComponentMask(BitMask includeMask, BitMask excludeMask)
        {
            IncludeMask = includeMask;
            ExcludeMask = excludeMask;
        }

        public override bool Equals(object? obj)
        {
            return obj is ComponentMask c && Equals(c);
        }

        public bool Equals(ComponentMask? other)
        {
            if (ReferenceEquals(null, other)) return false;
            return other.IncludeMask.Equals(IncludeMask) && other.ExcludeMask.Equals(ExcludeMask);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override string? ToString()
        {
            return $"""
            Included: {IncludeMask.ToString()}
            Excluded: {ExcludeMask.ToString()}
            """;
        }

        public static ComponentMaskBuilder Create()
        {
            BitMask incMask = new();
            BitMask excMask = new();
            return new ComponentMaskBuilder(incMask, excMask);
        }

#pragma warning disable CA1034 // Nested types should not be visible
#pragma warning disable CA1815 // Override equals and operator equals on value types
        public struct ComponentMaskBuilder
#pragma warning restore CA1815 // Override equals and operator equals on value types
#pragma warning restore CA1034 // Nested types should not be visible
        {
            public BitMask IncludeMask;
            public BitMask ExcludeMask;

            public ComponentMaskBuilder(BitMask includeMask, BitMask excludeMask)
            {
                IncludeMask = includeMask;
                ExcludeMask = excludeMask;
            }

            [UnscopedRef]
            public ref ComponentMaskBuilder Inc<T>() where T : struct, IComponent<T>
            {
                IncludeMask.SetBit(World.GetOrCreateTypeId<T>());
                return ref this;
            }

            [UnscopedRef]
            public ref ComponentMaskBuilder Exc<T>() where T : struct, IComponent<T>
            {
                ExcludeMask.SetBit(World.GetOrCreateTypeId<T>());
                return ref this;
            }

            public ComponentMask End()
            {
                return new ComponentMask(IncludeMask, ExcludeMask);
            }
        }
    }
}

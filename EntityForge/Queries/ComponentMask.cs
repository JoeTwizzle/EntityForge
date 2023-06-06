using EntityForge.Collections;
using System.Diagnostics.CodeAnalysis;

namespace EntityForge
{
    public sealed class ComponentMask : IEquatable<ComponentMask>
    {
        public readonly BitMask[] SomeMasks;
        public readonly BitMask HasMask;
        public readonly BitMask WriteMask;
        public readonly BitMask ExcludeMask;

        public ComponentMask(BitMask[] someMasks, BitMask hasMask, BitMask accessMask, BitMask excludeMask)
        {
            SomeMasks = someMasks;
            HasMask = hasMask;
            WriteMask = accessMask;
            ExcludeMask = excludeMask;
        }

        public override bool Equals(object? obj)
        {
            return obj is ComponentMask c && Equals(c);
        }

        public bool Equals(ComponentMask? other)
        {
            if (ReferenceEquals(null, other)) return false;
            bool hasSome = SomeMasks.Length == other.SomeMasks.Length;
            if (hasSome)
            {
                for (int i = 0; i < SomeMasks.Length; i++)
                {
                    hasSome &= SomeMasks[i].Equals(other.SomeMasks[i]);
                }
            }
            return hasSome && other.HasMask.Equals(HasMask) && other.ExcludeMask.Equals(ExcludeMask);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override string? ToString()
        {
            return $"""
            Has: {HasMask.ToString()}
            Write: {WriteMask.ToString()}
            Some: {string.Join<BitMask>(" - ", SomeMasks)}
            Excluded: {ExcludeMask.ToString()}
            """;
        }

        public static ComponentMaskBuilder Create()
        {
            return new ComponentMaskBuilder(new List<BitMask>(), new(), new(), new());
        }

#pragma warning disable CA1034 // Nested types should not be visible
#pragma warning disable CA1815 // Override equals and operator equals on value types
        public struct ComponentMaskBuilder
#pragma warning restore CA1815 // Override equals and operator equals on value types
#pragma warning restore CA1034 // Nested types should not be visible
        {
            internal readonly List<BitMask> SomeMasks;
            internal readonly BitMask HasMask;
            internal readonly BitMask WriteMask;
            internal readonly BitMask ExcludeMask;

            internal ComponentMaskBuilder(List<BitMask> someMasks, BitMask hasMask, BitMask writeMask, BitMask excludeMask)
            {
                SomeMasks = someMasks;
                HasMask = hasMask;
                WriteMask = writeMask;
                ExcludeMask = excludeMask;
            }

            [UnscopedRef]
            public ref ComponentMaskBuilder Read<T>() where T : struct, IComponent<T>
            {
                HasMask.SetBit(World.GetOrCreateTypeId<T>());
                return ref this;
            }

            [UnscopedRef]
            public ref ComponentMaskBuilder Write<T>() where T : struct, IComponent<T>
            {
                HasMask.SetBit(World.GetOrCreateTypeId<T>());
                WriteMask.SetBit(World.GetOrCreateTypeId<T>());
                return ref this;
            }

            [UnscopedRef]
            public ref ComponentMaskBuilder Exc<T>() where T : struct, IComponent<T>
            {
                ExcludeMask.SetBit(World.GetOrCreateTypeId<T>());
                return ref this;
            }

            public SomeMaskBuilder Some()
            {
                return new SomeMaskBuilder(this, new(), new());
            }

            public ComponentMask End()
            {
                return new ComponentMask(SomeMasks.ToArray(), HasMask, WriteMask, ExcludeMask);
            }
        }

#pragma warning disable CA1034 // Nested types should not be visible
#pragma warning disable CA1815 // Override equals and operator equals on value types
        public struct SomeMaskBuilder
#pragma warning restore CA1815 // Override equals and operator equals on value types
#pragma warning restore CA1034 // Nested types should not be visible
        {
            internal readonly BitMask SomeMask;
            internal readonly BitMask AccessMask;
            private readonly ComponentMaskBuilder parent;

            public SomeMaskBuilder(ComponentMaskBuilder parent, BitMask someMask, BitMask accessMask)
            {
                this.parent = parent;
                SomeMask = someMask;
                AccessMask = accessMask;
            }

            [UnscopedRef]
            public ref SomeMaskBuilder Read<T>() where T : struct, IComponent<T>
            {
                SomeMask.SetBit(World.GetOrCreateTypeId<T>());
                return ref this;
            }

            [UnscopedRef]
            public ref SomeMaskBuilder Write<T>() where T : struct, IComponent<T>
            {
                SomeMask.SetBit(World.GetOrCreateTypeId<T>());
                AccessMask.SetBit(World.GetOrCreateTypeId<T>());
                return ref this;
            }


            public ComponentMaskBuilder EndSome()
            {
                parent.SomeMasks.Add(SomeMask);
                parent.WriteMask.OrBits(AccessMask);
                return parent;
            }
        }
    }
}

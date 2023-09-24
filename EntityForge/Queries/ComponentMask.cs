using EntityForge.Collections;
using System.Diagnostics.CodeAnalysis;

namespace EntityForge
{
    public sealed class ComponentMask : IEquatable<ComponentMask>
    {
        public readonly BitMask[] SomeOfMasks; //per index must have one of the components 
        public readonly BitMask[] NotAllMasks; //per index must have none of the components 
        public readonly BitMask HasMask;
        public readonly BitMask WriteMask;
        public readonly BitMask ExcludeMask;

        public ComponentMask(BitMask[] someMasks, BitMask[] noneMasks, BitMask hasMask, BitMask writeMask, BitMask excludeMask)
        {
            SomeOfMasks = someMasks;
            NotAllMasks = noneMasks;
            HasMask = hasMask;
            WriteMask = writeMask;
            ExcludeMask = excludeMask;
        }

        public override bool Equals(object? obj)
        {
            return obj is ComponentMask c && Equals(c);
        }

        public bool Equals(ComponentMask? other)
        {
            if (other is null) return false;
            return other.HasMask.Equals(HasMask) && other.ExcludeMask.Equals(ExcludeMask) && SomeOfMasks.Equals(other.SomeOfMasks) && NotAllMasks.Equals(other.NotAllMasks);
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
            Some: {string.Join<BitMask>(" - ", SomeOfMasks)}
            Not all: {string.Join<BitMask>(" - ", NotAllMasks)}
            Excluded: {ExcludeMask.ToString()}
            """;
        }

        public static ComponentMaskBuilder Create()
        {
            return new ComponentMaskBuilder(new List<BitMask>(), new List<BitMask>(), new(), new(), new());
        }

#pragma warning disable CA1034 // Nested types should not be visible
        public struct ComponentMaskBuilder : IEquatable<ComponentMaskBuilder>
        {
            internal readonly List<BitMask> SomeMasks;
            internal readonly List<BitMask> NoneMasks;
            internal readonly BitMask HasMask;
            internal readonly BitMask WriteMask;
            internal readonly BitMask ExcludeMask;

            internal ComponentMaskBuilder(List<BitMask> someMasks, List<BitMask> noneMasks, BitMask hasMask, BitMask writeMask, BitMask excludeMask)
            {
                SomeMasks = someMasks;
                NoneMasks = noneMasks;
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

            public NotAllMaskBuilder NotAll()
            {
                return new NotAllMaskBuilder(this, new());
            }

            public ComponentMask End()
            {
                return new ComponentMask(SomeMasks.ToArray(), NoneMasks.ToArray(), HasMask, WriteMask, ExcludeMask); //TODO
            }



            public override bool Equals(object? obj)
            {
                return obj is ComponentMaskBuilder builder && Equals(builder);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(SomeMasks, HasMask, WriteMask, ExcludeMask);
            }

            public bool Equals(ComponentMaskBuilder other)
            {
                bool hasSome = SomeMasks.Count == other.SomeMasks.Count;
                if (hasSome)
                {
                    for (int i = 0; i < SomeMasks.Count; i++)
                    {
                        hasSome &= SomeMasks[i].Equals(other.SomeMasks[i]);
                    }
                }
                return hasSome && other.HasMask.Equals(HasMask) && other.ExcludeMask.Equals(ExcludeMask);
            }

            public static bool operator ==(ComponentMaskBuilder left, ComponentMaskBuilder right)
            {
                return left.Equals(right);
            }

            public static bool operator !=(ComponentMaskBuilder left, ComponentMaskBuilder right)
            {
                return !(left == right);
            }
        }

        public struct SomeMaskBuilder : IEquatable<SomeMaskBuilder>
        {
            internal readonly BitMask SomeMask;
            internal readonly BitMask WriteMask;
            private readonly ComponentMaskBuilder parent;

            public SomeMaskBuilder(ComponentMaskBuilder parent, BitMask someMask, BitMask accessMask)
            {
                this.parent = parent;
                SomeMask = someMask;
                WriteMask = accessMask;
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
                WriteMask.SetBit(World.GetOrCreateTypeId<T>());
                return ref this;
            }

            public ComponentMaskBuilder EndSome()
            {
                parent.SomeMasks.Add(SomeMask);
                parent.WriteMask.OrBits(WriteMask);
                return parent;
            }


            public override bool Equals(object? obj)
            {
                return obj is SomeMaskBuilder other && Equals(other);
            }

            public override int GetHashCode()
            {
                return base.GetHashCode();
            }

            public override string? ToString()
            {
                return base.ToString();
            }

            public static bool operator ==(SomeMaskBuilder left, SomeMaskBuilder right)
            {
                return left.Equals(right);
            }

            public static bool operator !=(SomeMaskBuilder left, SomeMaskBuilder right)
            {
                return !(left == right);
            }

            public bool Equals(SomeMaskBuilder other)
            {
                return SomeMask.Equals(other.SomeMask) && WriteMask.Equals(other.WriteMask);
            }
        }

        public struct NotAllMaskBuilder : IEquatable<NotAllMaskBuilder>
        {
            internal readonly BitMask NoneMask;
            private readonly ComponentMaskBuilder parent;

            public NotAllMaskBuilder(ComponentMaskBuilder parent, BitMask noneMask)
            {
                this.parent = parent;
                NoneMask = noneMask;
            }

            [UnscopedRef]
            public ref NotAllMaskBuilder Exc<T>() where T : struct, IComponent<T>
            {
                NoneMask.SetBit(World.GetOrCreateTypeId<T>());
                return ref this;
            }

            public ComponentMaskBuilder EndSome()
            {
                parent.NoneMasks.Add(NoneMask);
                return parent;
            }

            public override bool Equals(object? obj)
            {
                return obj is NotAllMaskBuilder other && Equals(other);
            }

            public override int GetHashCode()
            {
                return base.GetHashCode();
            }

            public override string? ToString()
            {
                return base.ToString();
            }

            public static bool operator ==(NotAllMaskBuilder left, NotAllMaskBuilder right)
            {
                return left.Equals(right);
            }

            public static bool operator !=(NotAllMaskBuilder left, NotAllMaskBuilder right)
            {
                return !(left == right);
            }

            public bool Equals(NotAllMaskBuilder other)
            {
                return NoneMask.Equals(other.NoneMask);
            }
        }
#pragma warning restore CA1034 // Nested types should not be visible
    }
}

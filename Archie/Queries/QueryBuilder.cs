using System.Diagnostics.CodeAnalysis;

namespace Archie.Queries
{
    struct QueryInfo
    {
        int typeId;
        byte flags;

        public QueryInfo(int typeId, byte flags)
        {
            this.typeId = typeId;
            this.flags = flags;
        }
    }

    public struct QueryBuilder : IEquatable<QueryBuilder>
    {
        ComponentMask mask;

        [UnscopedRef]
        public ref QueryBuilder Inc<T>() where T : struct, IComponent<T>
        {
            mask.HasMask.SetBit(World.GetOrCreateTypeId<T>());
            return ref this;
        }

        [UnscopedRef]
        public ref QueryBuilder Exc<T>() where T : struct, IComponent<T>
        {
            mask.ExcludeMask.SetBit(World.GetOrCreateTypeId<T>());
            return ref this;
        }

        public ComponentMask End()
        {
            return mask;
        }

        public override bool Equals(object? obj)
        {
            return obj is QueryBuilder q && Equals(q);
        }

        public override int GetHashCode()
        {
            return mask.GetHashCode();
        }

        public static bool operator ==(QueryBuilder left, QueryBuilder right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(QueryBuilder left, QueryBuilder right)
        {
            return !(left == right);
        }

        public bool Equals(QueryBuilder other)
        {
            return mask.Equals(other.mask);
        }
    }
}

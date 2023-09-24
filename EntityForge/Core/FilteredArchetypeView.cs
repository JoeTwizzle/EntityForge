using EntityForge.Collections;
using System.Diagnostics;

namespace EntityForge
{
    public struct FilteredArchetypeView : IEquatable<FilteredArchetypeView>
    {
        public readonly Archetype Archetype;
        public readonly BitMask AccessMask;
        public readonly BitMask FilterMask;
        public readonly int Length;

        public FilteredArchetypeView(Archetype archetype, BitMask accessMask, BitMask filterMask, int length)
        {
            Archetype = archetype;
            AccessMask = accessMask;
            FilterMask = filterMask;
            Length = length;
        }

        public ReadOnlySpan<Entity> Entities => Archetype.Entities;

        public ReadOnlySpan<long> MatchingEntities => FilterMask.Bits;

        public ReadOnlySpan<T> GetRead<T>() where T : struct, IComponent<T>
        {
            Debug.Assert(!AccessMask.IsSet(World.GetOrCreateTypeId<T>()), "Trying to get a read/write component pool with readonly access.");
            return Archetype.GetPool<T>();
        }

        public Span<T> GetWrite<T>() where T : struct, IComponent<T>
        {
            Debug.Assert(AccessMask.IsSet(World.GetOrCreateTypeId<T>()), "Trying to get a readonly component pool with read/write access.");
            return Archetype.GetPool<T>();
        }

        public override bool Equals(object? obj)
        {
            return obj is FilteredArchetypeView p && Equals(p);
        }

        public override int GetHashCode()
        {
            return Archetype.GetHashCode();
        }

        public static bool operator ==(FilteredArchetypeView left, FilteredArchetypeView right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(FilteredArchetypeView left, FilteredArchetypeView right)
        {
            return !(left == right);
        }

        public bool Equals(FilteredArchetypeView other)
        {
            return Archetype.Equals(other.Archetype) && AccessMask.Equals(other.AccessMask) && FilterMask.Equals(other.FilterMask);
        }
    }
}

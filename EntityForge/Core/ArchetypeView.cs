using EntityForge.Collections;
using System.Diagnostics;

namespace EntityForge
{
    public readonly struct ArchetypeView : IEquatable<ArchetypeView>
    {
        public readonly Archetype Archetype;
        public readonly BitMask AccessMask;
        public readonly int Length;

        public ArchetypeView(Archetype archetype, BitMask accessMask, int length)
        {
            Archetype = archetype;
            AccessMask = accessMask;
            Length = length;
            Debug.Assert(length <= archetype.EntityCount);
        }

        public ArchetypeView(Archetype archetype, BitMask accessMask)
        {
            Archetype = archetype;
            AccessMask = accessMask;
            Length = archetype.EntityCount;
        }

        public ReadOnlySpan<Entity> Entities => Archetype.Entities;

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
            return obj is ArchetypeView p && Equals(p);
        }

        public override int GetHashCode()
        {
            return Archetype.GetHashCode();
        }

        public static bool operator ==(ArchetypeView left, ArchetypeView right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ArchetypeView left, ArchetypeView right)
        {
            return !(left == right);
        }

        public bool Equals(ArchetypeView other)
        {
            return Archetype.Equals(other.Archetype) && AccessMask.Equals(other.AccessMask);
        }
    }
}

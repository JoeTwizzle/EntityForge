﻿using Archie.Collections;
using System.Diagnostics;

namespace Archie
{
    public struct ArchetypeView : IEquatable<ArchetypeView>
    {
        public readonly Archetype Archetype;
        public readonly BitMask AccessMask;

        public ArchetypeView(Archetype archetype, BitMask accessMask)
        {
            Archetype = archetype;
            AccessMask = accessMask;
        }

        public ReadOnlySpan<Entity> Entities => Archetype.Entities;

        public ReadOnlySpan<T> GetRead<T>() where T : struct, IComponent<T>
        {
            Debug.Assert(!AccessMask.IsSet(World.GetOrCreateTypeId<T>()));
            return Archetype.GetPool<T>();
        }

        public Span<T> GetWrite<T>() where T : struct, IComponent<T>
        {
            Debug.Assert(AccessMask.IsSet(World.GetOrCreateTypeId<T>()));
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

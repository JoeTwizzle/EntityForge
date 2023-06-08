﻿using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace EntityForge
{
    [StructLayout(LayoutKind.Explicit, Size = 8)]
    public struct PackedEntity : IEquatable<PackedEntity>
    {
        [FieldOffset(0)]
        public int Entity;
        [FieldOffset(4)]
        public short World;
        [FieldOffset(6)]
        public short Version;

        [SkipLocalsInit]
        public PackedEntity(int entity, short version, short world)
        {
            Entity = entity;
            Version = version;
            World = world;
        }

        public override bool Equals(object? obj)
        {
            return obj is PackedEntity id && Equals(id);
        }

        public bool Equals(PackedEntity other)
        {
            return Entity == other.Entity && World == other.World && Version == other.Version;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 486187739 + Entity;
                hash = hash * 486187739 + World;
                hash = hash * 486187739 + Version;
                return hash;
            }
        }

        public EntityId ToEntityId()
        {
            return new EntityId(Entity);
        }

        public static implicit operator EntityId(PackedEntity id)
        {
            return new EntityId(id.Entity);
        }

        //public static implicit operator PackedEntity(ulong id)
        //{
        //    return new PackedEntity(id);
        //}

        public static bool operator ==(PackedEntity left, PackedEntity right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(PackedEntity left, PackedEntity right)
        {
            return !(left == right);
        }
    }
}

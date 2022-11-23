using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Archie
{
    [StructLayout(LayoutKind.Explicit, Size = 8)]
    public struct EntityId : IEquatable<EntityId>, IEquatable<ulong>
    {
        public ulong Id
        {
            get
            {
                return Unsafe.As<uint, ulong>(ref Entity);
            }
            set
            {
                Unsafe.As<uint, ulong>(ref Entity) = Id;
            }
        }

        [FieldOffset(0)]
        public uint Entity;
        [FieldOffset(4)]
        public ushort Version;
        [FieldOffset(6)]
        public byte World;
        [FieldOffset(7)]
        public byte Special;

        [SkipLocalsInit]
        public EntityId(ulong Id)
        {
            Unsafe.As<uint, ulong>(ref Entity) = Id;
        }

        [SkipLocalsInit]
        public EntityId(uint entity, ushort version, byte world, byte special = 0)
        {
            Entity = entity;
            Version = version;
            World = world;
            Special = special;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EntityId Recycle(EntityId entity)
        {
            var ent = new EntityId(entity.Id);
            ent.Version++;
            return ent;
        }


        public override bool Equals(object? obj)
        {
            return obj is EntityId id &&
                   Id == id.Id;
        }

        public bool Equals(EntityId other)
        {
            return Id == other.Id;
        }

        public override int GetHashCode()
        {
            return (int)Id;
        }

        public bool Equals(ulong other)
        {
            return Id == other;
        }

        public static implicit operator ulong(EntityId id)
        {
            return id.Id;
        }

        public static implicit operator EntityId(ulong id)
        {
            return new EntityId(id);
        }

        public static bool operator ==(EntityId left, EntityId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(EntityId left, EntityId right)
        {
            return !(left == right);
        }
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Archie
{
    public readonly struct EntityId : IEquatable<EntityId>, IEquatable<ulong>
    {
        public readonly ulong Id;
        public uint Entity => (uint)Id;
        public ushort Version => (ushort)(Id >>> 4 * 8);
        public byte World => (byte)(Id >>> 6 * 8);
        public byte Special => (byte)(Id >>> 7 * 8);

        public EntityId(ulong id)
        {
            Id = id;
        }

        public EntityId(uint entity, ushort version, byte world, byte special = 0)
        {
            Id = (entity | ((ulong)version << 4 * 8) | ((ulong)world << 6 * 8) | ((ulong)special << 7 * 8));
        }

        public static EntityId Recycle(EntityId entity)
        {
            return new EntityId(entity.Entity, (ushort)(entity.Version + 1), entity.World, entity.Special);
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
    //public readonly struct Type : IEquatable<Type>, IEquatable<ulong>
    //{
    //    public readonly ulong Id;

    //    public Type(ulong id)
    //    {
    //        Id = id;
    //    }

    //    public override bool Equals(object? obj)
    //    {
    //        return obj is Type id &&
    //               Id == id.Id;
    //    }

    //    public bool Equals(Type other)
    //    {
    //        return Id == other.Id;
    //    }

    //    public bool Equals(ulong other)
    //    {
    //        return Id == other;
    //    }

    //    public override ulong GetHashCode()
    //    {
    //        return (int)Id;
    //    }

    //    public static implicit operator Type(ulong id)
    //    {
    //        return new Type(id);
    //    }

    //    public static implicit operator ulong(Type id)
    //    {
    //        return id.Id;
    //    }

    //    public static bool operator ==(Type left, Type right)
    //    {
    //        return left.Equals(right);
    //    }

    //    public static bool operator !=(Type left, Type right)
    //    {
    //        return !(left == right);
    //    }
    //}
}

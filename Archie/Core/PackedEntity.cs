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
    public struct PackedEntity : IEquatable<PackedEntity>, IEquatable<ulong>
    {
        public ulong Id
        {
            get
            {
                return Unsafe.As<int, ulong>(ref Entity);
            }
            set
            {
                Unsafe.As<int, ulong>(ref Entity) = Id;
            }
        }

        [FieldOffset(0)]
        public int Entity;
        [FieldOffset(4)]
        public short Version;
        [FieldOffset(6)]
        public byte World;
        [FieldOffset(7)]
        public byte Special;

        [SkipLocalsInit]
        public PackedEntity(ulong Id)
        {
            Unsafe.As<int, ulong>(ref Entity) = Id;
        }

        [SkipLocalsInit]
        public PackedEntity(int entity, short version, byte world, byte special = 0)
        {
            Entity = entity;
            Version = version;
            World = world;
            Special = special;
        }

        public override bool Equals(object? obj)
        {
            return obj is PackedEntity id &&
                   Id == id.Id;
        }

        public bool Equals(PackedEntity other)
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

        //public static implicit operator ulong(PackedEntity id)
        //{
        //    return id.Id;
        //}

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

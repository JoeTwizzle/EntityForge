namespace Archie
{
    public readonly struct ArchitypeId : IEquatable<ArchitypeId>, IEquatable<int>
    {
        public readonly int Id;

        public ArchitypeId(int id)
        {
            Id = id;
        }

        public override bool Equals(object? obj)
        {
            return obj is ArchitypeId id &&
                   Id == id.Id;
        }

        public bool Equals(ArchitypeId other)
        {
            return Id == other.Id;
        }
        public bool Equals(int other)
        {
            return Id == other;
        }

        public override int GetHashCode()
        {
            return (int)Id;
        }

        public static implicit operator int(ArchitypeId id)
        {
            return id.Id;
        }

        public static implicit operator ArchitypeId(int id)
        {
            return new ArchitypeId(id);
        }

        public static bool operator ==(ArchitypeId left, ArchitypeId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ArchitypeId left, ArchitypeId right)
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

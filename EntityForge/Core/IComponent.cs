using EntityForge.Relations;

namespace EntityForge
{
#pragma warning disable CA1000 // Do not declare static members on generic types
    //Constrain to where T : this if and when the language allows it
    public interface IComponent<T> where T : struct, IComponent<T>
    {
        /// <summary>
        /// DO NOT OVERRIDE
        /// </summary>
        public static virtual bool Registered { get; set; }
        /// <summary>
        /// DO NOT OVERRIDE
        /// </summary>
        public static virtual int Id { get; set; }
    }
#pragma warning restore CA1000 // Do not declare static members on generic types

    //internal struct Rel<T> : IEquatable<Rel<T>>, IEquatable<T>, IComponent<Rel<T>> where T : struct, IComponent<T>
    //{
    //    public TreeRelation TreeRelation;

    //    public T Data;

    //    public Rel(T data)
    //    {
    //        Data = data;
    //    }

    //    public static implicit operator Rel<T>(T data) => new Rel<T>(data);

    //    public static Rel<T> ToRel(T data) => new Rel<T>(data);

    //    public override bool Equals(object? obj)
    //    {
    //        if (obj is Rel<T> r)
    //        {
    //            return Equals(r);
    //        }
    //        if (obj is T r2)
    //        {
    //            return Equals(r2);
    //        }
    //        return false;
    //    }

    //    public override int GetHashCode()
    //    {
    //        return Data.GetHashCode();
    //    }

    //    public static bool operator ==(Rel<T> left, Rel<T> right)
    //    {
    //        return left.Equals(right);
    //    }

    //    public static bool operator !=(Rel<T> left, Rel<T> right)
    //    {
    //        return !(left == right);
    //    }

    //    public bool Equals(Rel<T> other)
    //    {
    //        return Data.Equals(other.Data);
    //    }

    //    public bool Equals(T other)
    //    {
    //        return Data.Equals(other);
    //    }
    //}
}

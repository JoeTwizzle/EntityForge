﻿namespace Archie
{


#pragma warning disable CA1000 // Do not declare static members on generic types
#pragma warning disable CA1040 // Avoid empty interfaces
    //Constrain to where T : this if and when the language allows it
    public interface IComponent<T> where T : struct, IComponent<T>
    {
        public static virtual bool Registered { get; set; }
        public static virtual int Id { get; set; }
    }

    public enum RelationKind
    {
        Discriminated,
        SingleSingle,
        SingleMulti,
        MultiMulti,
    }

    public enum RelationProperty
    {
        None,
        Transitive,
        //Symmetric,
    }

    public interface IRelation<T> : IComponent<T> where T : struct, IRelation<T>
    {
        public static abstract RelationKind RelationKind { get; }
        public static virtual RelationProperty RelationProperty { get; } = RelationProperty.None;
    }
#pragma warning restore CA1040 // Avoid empty interfaces
#pragma warning restore CA1000 // Do not declare static members on generic types
}

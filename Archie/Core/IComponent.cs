namespace Archie
{


#pragma warning disable CA1000 // Do not declare static members on generic types
#pragma warning disable CA1040 // Avoid empty interfaces
    //Constrain to where T : this if and when the language allows it
    public interface IComponent<T> where T : struct, IComponent<T>
    {
        public static virtual bool Registered { get; set; }
        public static virtual int Id { get; set; }
        //static virtual void Init(ref T self) { }
        //static virtual void Del(ref T self) { self.Dispose(); }
    }

    public enum RelationKind
    {
        SingleSingleDiscriminated,
        SingleSingle,
        SingleMulti,
        MultiMulti,
    }

    public interface IRelation<T> : IComponent<T> where T : struct, IRelation<T>, IComponent<T>
    {
        public static abstract RelationKind RelationKind { get; }
    }
#pragma warning restore CA1040 // Avoid empty interfaces
#pragma warning restore CA1000 // Do not declare static members on generic types

}

using Archie.Relations;

namespace Archie
{


#pragma warning disable CA1000 // Do not declare static members on generic types
#pragma warning disable CA1040 // Avoid empty interfaces

    //Constrain to where T : this if and when the language allows it
    public interface IRegisterableType<T> where T : struct, IRegisterableType<T>
    {
        public static virtual bool Registered { get; set; }
        public static virtual int Id { get; set; }
    }

    //Constrain to where T : this if and when the language allows it
    public interface IComponent<T> : IRegisterableType<T> where T : struct, IComponent<T>
    {
        public static virtual void OnInit(ref T self) { }
        public static virtual void OnDelete(ref T self) { }
    }

    public interface ITreeRelation<T> : IComponent<T> where T : struct, ITreeRelation<T>
    {
        public ref TreeRelation GetRelation();
    }

    public interface ISingleRelation<T> : IRegisterableType<T> where T : struct, ISingleRelation<T>
    {
        public ref SingleRelation GetRelation();
    }
#pragma warning restore CA1040 // Avoid empty interfaces
#pragma warning restore CA1000 // Do not declare static members on generic types
}

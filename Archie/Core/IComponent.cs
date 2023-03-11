using Archie.Relations;
using System.Runtime.CompilerServices;

namespace Archie
{


#pragma warning disable CA1000 // Do not declare static members on generic types
#pragma warning disable CA1040 // Avoid empty interfaces

    //Constrain to where T : this if and when the language allows it
    public interface IComponent<T> where T : struct, IComponent<T>
    {
        public static virtual bool Registered { get; set; }
        public static virtual int Id { get; set; }
        public static virtual void OnInit(ref T self) { }
        public static virtual void OnDelete(ref T self) { }

#pragma warning disable CA1707 // Identifiers should not contain underscores
        [Obsolete("DO NOT INVOKE!! Used for internal initialization")]
        public static unsafe void __InternalOnInit(Array buffer, int self)
        {
            T.OnInit(ref ((T[])buffer)[self]);
        }
        [Obsolete("DO NOT INVOKE!! Used for internal deletion")]
        public static unsafe void __InternalOnDelete(Array buffer, int self)
        {
            T.OnDelete(ref ((T[])buffer)[self]);
        }
#pragma warning restore CA1707 // Identifiers should not contain underscores
    }

    public interface ITreeRelation<T> : IComponent<T> where T : struct, ITreeRelation<T>
    {
        public ref TreeRelation GetRelation();
    }

    public interface ISingleRelation<T> : IComponent<T> where T : struct, ISingleRelation<T>
    {
        public ref SingleRelation GetRelation();
    }
#pragma warning restore CA1040 // Avoid empty interfaces
#pragma warning restore CA1000 // Do not declare static members on generic types
}

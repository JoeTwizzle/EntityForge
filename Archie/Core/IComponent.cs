namespace Archie
{


#pragma warning disable CS0618 // Type or member is obsolete
#pragma warning disable CA1000 // Do not declare static members on generic types
    //Constrain to where T : this if and when the language allows it
    public interface IComponent<T> where T : struct, IComponent<T>
    {
        //static virtual void Init(ref T self) { }
        //static virtual void Del(ref T self) { self.Dispose(); }
    }
#pragma warning restore CA1000 // Do not declare static members on generic types
#pragma warning restore CS0618 // Type or member is obsolete
}

using System.Runtime.CompilerServices;

namespace EntityForge
{

    public interface IComponentQuery<T> where T : struct, IComponent
    {
        
        void Process(ref T item);
    }

    public interface IComponentQuery<T, T2> where T : struct, IComponent where T2 : struct, IComponent
    {
        
        void Process(ref T item, ref T2 item2);
    }

    public interface IComponentQuery<T, T2, T3> where T : struct, IComponent where T2 : struct, IComponent where T3 : struct, IComponent
    {
        
        void Process(ref T item, ref T2 item2, ref T3 item3);
    }

    public interface IComponentQuery<T, T2, T3, T4> where T : struct, IComponent where T2 : struct, IComponent where T3 : struct, IComponent where T4 : struct, IComponent
    {
        
        void Process(ref T item, ref T2 item2, ref T3 item3, ref T4 item4);
    }

    public interface IComponentQuery<T, T2, T3, T4, T5> where T : struct, IComponent where T2 : struct, IComponent where T3 : struct, IComponent where T4 : struct, IComponent where T5 : struct, IComponent
    {
        
        void Process(ref T item, ref T2 item2, ref T3 item3, ref T4 item4, ref T5 item5);
    }
}

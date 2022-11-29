using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Archie
{
    public interface IComponentQuery<T> where T : struct, IComponent<T>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void Process(ref T item);
    }

    public interface IComponentQuery<T, T2> where T : struct, IComponent<T> where T2 : struct, IComponent<T2>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void Process(ref T item, ref T2 item2);
    }

    public interface IComponentQuery<T, T2, T3> where T : struct, IComponent<T> where T2 : struct, IComponent<T2> where T3 : struct, IComponent<T3>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void Process(ref T item, ref T2 item2, ref T3 item3);
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Archie
{
    //Constrain to where T : this if and when the language allows it
    public interface IComponent<T> where T : struct, IComponent<T>
    {
#pragma warning disable CA1000 // Do not declare static members on generic types
        static virtual void OnAdd(ref T self, World world, EntityId entity) { }
        static virtual void OnRemove(ref T self, World world, EntityId entity) { }
        static virtual void Init(ref T self) { }
        static virtual void Del(ref T self) { }
#pragma warning restore CA1000 // Do not declare static members on generic types
    }
}

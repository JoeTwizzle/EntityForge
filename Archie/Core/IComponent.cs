using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Archie
{
    public interface IComponent<T> where T : struct, IComponent<T>
    {
        static virtual void Init(ref T self) { }
        static virtual void Del(ref T self) { }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Archie.Relations
{
    internal struct Relation<T> : IComponent<T> where T : struct, IComponent<T>
    {
        public T RelationType;
        public int TargetEntity;
    }
}

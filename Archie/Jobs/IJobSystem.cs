using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Archie.Jobs
{
    public interface IJobSystem<T> where T : struct, IJobSystem<T>
    {
        void Execute(Archetype archetype);
    }
}

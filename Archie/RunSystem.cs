using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Archie
{
    public interface IRunSystem
    {
        void Run();
    }
    public interface IParllelRunSystem
    {
        Filter Filter { get; }
        void PerEntity(EntityId id);
    }
}

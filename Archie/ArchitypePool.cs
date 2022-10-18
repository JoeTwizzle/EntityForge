using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Archie
{
    public struct Architype
    {
        Type[] memberTypes;
    }
    internal class ArchitypePool
    {
        Architype architype;
        EntityContext Context;
        IComponentPool[] pools;


    }
}

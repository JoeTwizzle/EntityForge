using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Archie
{
    internal readonly struct EntityRecord
    {
        public readonly List<EntityId> Entities = new();
        public EntityRecord()
        {
            Entities = new();
        }
    }
}

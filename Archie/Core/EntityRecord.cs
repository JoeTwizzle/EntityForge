using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Archie
{
    internal struct EntityRecord
    {
        public EntityId[] Entities;
        public EntityRecord()
        {
            Entities = new EntityId[256];
        }
    }
}

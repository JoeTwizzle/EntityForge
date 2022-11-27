using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Archie
{
    public readonly struct EntityId
    {
        public readonly int Id;

        public EntityId(int id)
        {
            Id = id;
        }

        public static implicit operator EntityId(int id)
        {
            return new EntityId(id);
        }
    }
}

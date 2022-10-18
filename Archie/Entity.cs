using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Archie
{
    public struct Entity
    {
        public readonly int Id;
        public readonly int Gen;
        public readonly int ContextId;

        public static implicit operator EntityId(Entity e) => new(e.Id);
    }
    public readonly struct EntityId
    {
        public readonly int Id;

        public EntityId(int id)
        {
            Id = id;
        }
        public static implicit operator int(EntityId e) => e.Id;
    }
}

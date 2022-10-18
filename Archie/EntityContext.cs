using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Archie
{
    internal class EntityContext
    {
        ArchitypePool[] architypePools;
        Entity[] entities;
        public Entity CreateEntity()
        {
            return entities[0];
        }
        public void DeleteEntity(EntityId entity)
        {

        }
        public void AddPool<T>() where T : struct
        {

        }
    }
}

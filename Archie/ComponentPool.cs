using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Archie
{
    public interface IComponentPool
    {
        public object AddRaw(EntityId entityId);
        public object GetRaw(EntityId entityId);
        public void RemoveRaw(EntityId entityId);
    }

    public sealed class Pool<T> : IComponentPool where T : struct
    {
        GrowArray<T> components;
        int[] entityToComponentMap;


        public object AddRaw(EntityId entityId)
        {
            return components[entityToComponentMap[entityId.Id]];
        }

        public ref T Add(EntityId entityId)
        {
            return ref components[entityToComponentMap[entityId.Id]];
        }


        public object GetRaw(EntityId entityId)
        {
            return components[entityToComponentMap[entityId.Id]];
        }

        public ref T Get(EntityId entityId)
        {
            return ref components[entityToComponentMap[entityId.Id]];
        }


        public void RemoveRaw(EntityId entityId)
        {

        }

        public void Remove(EntityId entityId)
        {

        }
    }
}

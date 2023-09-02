using EntityForge.Collections;
using EntityForge.Collections.Generic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EntityForge.SparseComponents
{
    internal sealed class SparseSetEcs : IDisposable
    {
        readonly MultiComponentList MultiComponentList = new();

        public void AddComponent<T>(EntityId entityId, T component) where T : struct, IComponent<T>
        {
            MultiComponentList.Add(entityId.Id, component);
        }

        public void RemoveComponent<T>(EntityId entityId) where T : struct, IComponent<T>
        {
            MultiComponentList.Remove<T>(entityId.Id);
        }

        public void RemoveComponent(EntityId entityId, ComponentInfo info)
        {
            MultiComponentList.Remove(entityId.Id, info);
        }

        public void Dispose()
        {
            MultiComponentList.Dispose();
        }
    }
}

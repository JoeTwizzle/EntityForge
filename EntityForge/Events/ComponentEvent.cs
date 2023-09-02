using EntityForge.Relations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EntityForge.Events
{
    public delegate void ComponentEvent<T>(EntityId entityId, ref T data) where T : struct, IComponent<T>;
    public delegate void ComponentEvent(EntityId entityId, ComponentInfo info);
}

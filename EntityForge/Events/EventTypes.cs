using EntityForge.Relations;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EntityForge.Events
{
    public delegate void EntityEvent(EntityId entityId);
    public delegate void ComponentEvent(EntityId entityId, int componentId);
    public delegate void TagEvent(EntityId entityId, int tagId);
}

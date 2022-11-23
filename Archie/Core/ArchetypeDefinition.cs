using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Archie
{
    public readonly struct ArchetypeDefinition
    {
        public readonly int HashCode;
        public readonly Type[] Types;

        internal ArchetypeDefinition(int hashCode, Type[] types)
        {
            HashCode = hashCode;
            Types = types;
        }

        public static ArchetypeDefinition Create(params Type[] types) => Archetype.CreateDefinition(types);
    }
}

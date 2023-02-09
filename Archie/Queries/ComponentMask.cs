using CommunityToolkit.HighPerformance.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Archie
{
    public sealed class ComponentMask : IEquatable<ComponentMask>
    {
        public readonly Type[] Included;
        public readonly Type[] Excluded;
        readonly int hashCode;

        public ComponentMask(Type[] included, Type[] excluded)
        {
            Included = included.Distinct().ToArray();
            Excluded = excluded.Distinct().ToArray();
            hashCode = World.GetComponentMaskHash(this);
        }

        public static ComponentMaskBuilder Create()
        {
            return ComponentMaskBuilder.Create();
        }

        public override bool Equals(object? obj)
        {
            return obj is ComponentMask && Equals((ComponentMask)obj);
        }

        public bool Equals(ComponentMask? other)
        {
            return other != null && hashCode == other.hashCode;
        }

        public override int GetHashCode()
        {
            return hashCode;
        }
    }
}

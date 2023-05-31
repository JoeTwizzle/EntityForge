using System.Diagnostics.CodeAnalysis;
//using static Archie.Commands.EcsCommandBuffer;

namespace Archie
{
    public struct ComponentMetaData : IEquatable<ComponentMetaData>
    {
        [MemberNotNullWhen(false, nameof(Type))]
        public bool IsUnmanaged => Type == null;
        public int TypeId;
        public int UnmanagedSize;
        public Type? Type;

        public override bool Equals(object? obj)
        {
            return obj is ComponentMetaData m && Equals(m);
        }

        public override int GetHashCode()
        {
            return TypeId;
        }

        public static bool operator ==(ComponentMetaData left, ComponentMetaData right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ComponentMetaData left, ComponentMetaData right)
        {
            return !(left == right);
        }

        public bool Equals(ComponentMetaData other)
        {
            return TypeId == other.TypeId;
        }
    }
}

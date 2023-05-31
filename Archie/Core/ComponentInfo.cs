using System.Runtime.CompilerServices;

namespace Archie
{
    public struct ComponentInfo : IEquatable<ComponentId>, IEquatable<ComponentInfo>
    {
        public bool IsUnmanaged => Type == null;

        public ComponentId ComponentId;
        public int UnmanagedSize;
        public Type? Type;

        public ComponentInfo(ComponentId componentId, Type type)
        {
            ComponentId = componentId;
            UnmanagedSize = 0;
            Type = type;
        }

        public ComponentInfo(ComponentId componentId, int unmanagedSize)
        {
            ComponentId = componentId;
            UnmanagedSize = unmanagedSize;
            Type = null;
        }

        public override bool Equals(object? obj)
        {
            return obj is ComponentId c && Equals(c);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode()
        {
            return ComponentId.GetHashCode();
        }

        public static bool operator ==(ComponentInfo left, ComponentId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ComponentInfo left, ComponentId right)
        {
            return !(left == right);
        }

        public static bool operator ==(ComponentId left, ComponentInfo right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ComponentId left, ComponentInfo right)
        {
            return !(left == right);
        }

        public bool Equals(ComponentId other)
        {
            return ComponentId == other;
        }

        public static bool operator ==(ComponentInfo left, ComponentInfo right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ComponentInfo left, ComponentInfo right)
        {
            return !(left == right);
        }

        public bool Equals(ComponentInfo other)
        {
            return ComponentId == other.ComponentId;
        }
    }
}

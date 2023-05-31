using System.Runtime.CompilerServices;

namespace Archie
{
    public struct ComponentInfo : IEquatable<int>, IEquatable<ComponentInfo>
    {
        public bool IsUnmanaged => Type == null;

        public int TypeId;
        public int UnmanagedSize;
        public Type? Type;

        public ComponentInfo(int typeId, Type type)
        {
            TypeId = typeId;
            UnmanagedSize = 0;
            Type = type;
        }

        public ComponentInfo(int typeId, int unmanagedSize)
        {
            TypeId = typeId;
            UnmanagedSize = unmanagedSize;
            Type = null;
        }

        public override bool Equals(object? obj)
        {
            return obj is int c && Equals(c);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode()
        {
            return TypeId.GetHashCode();
        }

        public static bool operator ==(ComponentInfo left, int right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ComponentInfo left, int right)
        {
            return !(left == right);
        }

        public static bool operator ==(int left, ComponentInfo right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(int left, ComponentInfo right)
        {
            return !(left == right);
        }

        public bool Equals(int other)
        {
            return TypeId == other;
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
            return TypeId == other.TypeId;
        }
    }
}

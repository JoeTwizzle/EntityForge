using System.Runtime.CompilerServices;

namespace Archie
{
    public struct ComponentId : IEquatable<ComponentId>
    {
        public ulong Id
        {
            get
            {
                return Unsafe.As<int, ulong>(ref TypeId);
            }
            set
            {
                Unsafe.As<int, ulong>(ref TypeId) = Id;
            }
        }
        public int TypeId;
        public int Variant;
        public Type Type;
        [SkipLocalsInit]
        public ComponentId(int typeId, int variant, Type type)
        {
            TypeId = typeId;
            Variant = variant;
            Type = type;
        }

        [SkipLocalsInit]
        public ComponentId(int typeId, Type type)
        {
            TypeId = typeId;
            Variant = 0;
            Type = type;
        }

        [SkipLocalsInit]
        public ComponentId(ulong id, Type type)
        {
            Id = id;
            Type = type;
        }

        public override bool Equals(object? obj)
        {
            return obj is ComponentId c && Equals(c);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 486187739 + TypeId;
                hash = hash * 486187739 + Variant;
                return hash;
            }
        }

        public static bool operator ==(ComponentId left, ComponentId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ComponentId left, ComponentId right)
        {
            return !(left == right);
        }

        public bool Equals(ComponentId other)
        {
            return Id == other.Id;
        }
    }
}

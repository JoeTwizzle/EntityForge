
namespace Archie
{
    public readonly struct ArchetypeDefinition : IEquatable<ArchetypeDefinition>
    {
        public readonly int HashCode;
        public readonly ComponentInfo[] ComponentIds;

        internal ArchetypeDefinition(int hashCode, ComponentInfo[] componentIds)
        {
            HashCode = hashCode;
            ComponentIds = componentIds;
        }

        public static ArchetypeBuilder Create()
        {
            return new ArchetypeBuilder();
        }

        public override bool Equals(object? obj)
        {
            return obj is ArchetypeDefinition a && Equals(a);
        }

        public override int GetHashCode()
        {
            return HashCode;
        }

        public static bool operator ==(ArchetypeDefinition left, ArchetypeDefinition right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ArchetypeDefinition left, ArchetypeDefinition right)
        {
            return !(left == right);
        }

        public bool Equals(ArchetypeDefinition other)
        {
            return other.HashCode == HashCode;
        }
    }
}

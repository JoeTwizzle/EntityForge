
namespace EntityForge
{
    public readonly struct ArchetypeDefinition : IEquatable<ArchetypeDefinition>
    {
        public static ArchetypeDefinition Empty => World.EmptyArchetypeDefinition;
        public readonly int HashCode;
        public readonly ReadOnlyMemory<ComponentInfo> ComponentInfos;

        internal ArchetypeDefinition(int hashCode, ReadOnlyMemory<ComponentInfo> componentInfos)
        {
            HashCode = hashCode;
            ComponentInfos = componentInfos;
        }

        public static ArchetypeBuilder Create()
        {
            return new ArchetypeBuilder(Array.Empty<ComponentInfo>());
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
            return other.HashCode == HashCode && ComponentInfos.Span.SequenceEqual(other.ComponentInfos.Span);
        }
    }
}

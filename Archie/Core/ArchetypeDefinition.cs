namespace Archie
{
    public readonly struct ArchetypeDefinition : IEquatable<ArchetypeDefinition>
    {
        public readonly int HashCode;
        public readonly Type[] Types;
        public readonly (Type, int)[] DiscriminatingRelations;

        internal ArchetypeDefinition(int hashCode, Type[] types, (Type, int)[] discriminatingRelations)
        {
            HashCode = hashCode;
            Types = types;
            DiscriminatingRelations = discriminatingRelations;
        }

        public static ArchetypeDefinition Create(params Type[] types) => Archetype.CreateDefinition(types);

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

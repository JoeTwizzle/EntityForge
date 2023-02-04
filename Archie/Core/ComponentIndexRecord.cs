namespace Archie
{
    /// <summary>
    /// Defines which index in the component arrays belongs to this entity
    /// </summary>
    public struct ComponentIndexRecord : IEquatable<ComponentIndexRecord>
    {
        /// <summary>
        /// The Archetype that contains entities
        /// </summary>
        public Archetype Archetype;
        /// <summary>
        /// The index in the component arrays that belong to this entity
        /// </summary>
        public int ArchetypeColumn;
        /// <summary>
        /// The version that the entity has, will be positive if the entity is alive
        /// </summary>
        public short EntityVersion;

        public ComponentIndexRecord(Archetype archetype, int archetypeColumn, short entityVersion)
        {
            Archetype = archetype;
            ArchetypeColumn = archetypeColumn;
            EntityVersion = entityVersion;
        }

        public override bool Equals(object? obj)
        {
            return obj is ComponentIndexRecord r && Equals(r);
        }

        public override int GetHashCode()
        {
            //Source: https://stackoverflow.com/questions/263400/what-is-the-best-algorithm-for-overriding-gethashcode 
            unchecked
            {
                int hash = 17;
                hash = hash * 486187739 + EntityVersion;
                hash = hash * 486187739 + ArchetypeColumn;
                hash = hash * 486187739 + Archetype.Hash;
                return hash;
            }
        }

        public static bool operator ==(ComponentIndexRecord left, ComponentIndexRecord right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ComponentIndexRecord left, ComponentIndexRecord right)
        {
            return !(left == right);
        }

        public bool Equals(ComponentIndexRecord other)
        {
            return other.EntityVersion == EntityVersion && other.ArchetypeColumn == ArchetypeColumn && other.Archetype == Archetype;
        }
    }
}

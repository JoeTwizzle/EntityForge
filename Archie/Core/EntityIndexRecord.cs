namespace Archie
{
    /// <summary>
    /// Defines which index in the component arrays belongs to this entity
    /// </summary>
    public struct EntityIndexRecord : IEquatable<EntityIndexRecord>
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

        public EntityIndexRecord(Archetype archetype, int archetypeColumn, short entityVersion)
        {
            Archetype = archetype;
            ArchetypeColumn = archetypeColumn;
            EntityVersion = entityVersion;
        }

        public override bool Equals(object? obj)
        {
            return obj is EntityIndexRecord r && Equals(r);
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

        public static bool operator ==(EntityIndexRecord left, EntityIndexRecord right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(EntityIndexRecord left, EntityIndexRecord right)
        {
            return !(left == right);
        }

        public bool Equals(EntityIndexRecord other)
        {
            return other.EntityVersion == EntityVersion && other.ArchetypeColumn == ArchetypeColumn && other.Archetype == Archetype;
        }
    }
}

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
        public int ComponentIndex;
        /// <summary>
        /// The version that the entity has, will be positive if the entity is alive
        /// </summary>
        public short EntityVersion;

        public ComponentIndexRecord(Archetype archetype, int componentIndex, short entityVersion)
        {
            Archetype = archetype;
            ComponentIndex = componentIndex;
            EntityVersion = entityVersion;
        }

        public override bool Equals(object? obj)
        {
            return obj is ComponentIndexRecord r && Equals(r);
        }

        public override int GetHashCode()
        {
            //Source: https://stackoverflow.com/questions/263400/what-is-the-best-algorithm-for-overriding-gethashcode 
            unchecked // Overflow is fine, just wrap
            {
                int hash = 17;
                // Suitable nullity checks etc, of course :)
                hash = hash * 486187739 + EntityVersion;
                hash = hash * 486187739 + ComponentIndex;
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
            return other.EntityVersion == EntityVersion && other.ComponentIndex == ComponentIndex && other.Archetype == Archetype;
        }
    }
}

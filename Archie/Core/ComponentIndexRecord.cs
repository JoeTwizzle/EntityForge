namespace Archie
{
    /// <summary>
    /// Defines which index in the component arrays belongs to this entity
    /// </summary>
    public struct ComponentIndexRecord
    {
        /// <summary>
        /// The Archetype that contains entities
        /// </summary>
        public Archetype Archetype;
        /// <summary>
        /// The index in the component arrays that belong to this entity
        /// </summary>
        public uint ComponentIndex;
        /// <summary>
        /// The version that the entity has, will be positive if the entity is alive
        /// </summary>
        public short EntityVersion;

        public ComponentIndexRecord(Archetype archetype, uint componentIndex, short entityVersion)
        {
            Archetype = archetype;
            ComponentIndex = componentIndex;
            EntityVersion = entityVersion;
        }
    }
}

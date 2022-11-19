namespace Archie
{
    /// <summary>
    /// Defines the siblings of this Archetype 
    /// </summary>
    public struct ArchetypeSiblings
    {
        /// <summary>
        /// The Archetype to move to when adding a component
        /// </summary>
        public Archetype? Add;
        /// <summary>
        /// The Archetype to move to when removing a component
        /// </summary>
        public Archetype? Remove;

        public ArchetypeSiblings(Archetype? add, Archetype? remove)
        {
            Add = add;
            Remove = remove;
        }
    }
}

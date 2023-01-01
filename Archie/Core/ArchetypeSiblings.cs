namespace Archie
{
    /// <summary>
    /// Defines the siblings of this Archetype 
    /// </summary>
    public struct ArchetypeSiblings : IEquatable<ArchetypeSiblings>
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

        public override bool Equals(object? obj)
        {
            return obj is ArchetypeSiblings a && Equals(a);
        }

        public override int GetHashCode()
        {
            //Source: https://stackoverflow.com/questions/263400/what-is-the-best-algorithm-for-overriding-gethashcode 
            unchecked // Overflow is fine, just wrap
            {
                int hash = 17;
                if (Add != null)
                {
                    hash = hash * 486187739 + Add.Hash;
                }
                if (Remove != null)
                {
                    hash = hash * 486187739 + Remove.Hash;
                }
                return hash;
            }
        }

        public static bool operator ==(ArchetypeSiblings left, ArchetypeSiblings right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ArchetypeSiblings left, ArchetypeSiblings right)
        {
            return !(left == right);
        }

        public bool Equals(ArchetypeSiblings other)
        {
            return other.Add == Add && other.Remove == Remove;
        }
    }
}

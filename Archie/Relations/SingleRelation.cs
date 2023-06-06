namespace EntityForge.Relations
{
    public struct SingleRelation : IEquatable<SingleRelation>
    {
        public EntityId? Target => targetInternal;
        internal EntityId? targetInternal;

        public override bool Equals(object? obj)
        {
            return obj is SingleRelation other && Equals(other);
        }

        public override int GetHashCode()
        {
            return targetInternal.GetHashCode();
        }

        public static bool operator ==(SingleRelation left, SingleRelation right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(SingleRelation left, SingleRelation right)
        {
            return !(left == right);
        }

        public bool Equals(SingleRelation other)
        {
            return targetInternal == other.targetInternal;
        }
    }
}

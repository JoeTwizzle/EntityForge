namespace Archie
{
    /// <summary>
    /// Defines which pool contains the component of said type
    /// </summary>
    public struct TypeIndexRecord : IEquatable<TypeIndexRecord>
    {
        /// <summary>     
        /// The index of the pool containing components
        /// </summary>
        public int ComponentTypeIndex;

        public TypeIndexRecord(int componentTypeIndex)
        {
            ComponentTypeIndex = componentTypeIndex;
        }

        public override bool Equals(object? obj)
        {
            return obj is TypeIndexRecord t && Equals(t);
        }

        public override int GetHashCode()
        {
            return (int)ComponentTypeIndex;
        }

        public static bool operator ==(TypeIndexRecord left, TypeIndexRecord right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(TypeIndexRecord left, TypeIndexRecord right)
        {
            return !(left == right);
        }

        public bool Equals(TypeIndexRecord other)
        {
            return other.ComponentTypeIndex == ComponentTypeIndex;
        }
    }
}

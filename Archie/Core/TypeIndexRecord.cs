namespace Archie
{
    /// <summary>
    /// Defines which pool contains the component of said type
    /// </summary>
    public struct TypeIndexRecord
    {
        /// <summary>     
        /// The index of the pool containing components
        /// </summary>
        public uint ComponentTypeIndex;

        public TypeIndexRecord(uint componentTypeIndex)
        {
            ComponentTypeIndex = componentTypeIndex;
        }
    }
}

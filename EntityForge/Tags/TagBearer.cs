using EntityForge.Collections;
using System.Runtime.CompilerServices;

namespace EntityForge.Tags
{
    internal struct TagBearer : IComponent<TagBearer>
    {
        internal BitMask mask;

        
        public void SetTag(int tagIndex)
        {
            if (mask is null)
            {
                mask = new BitMask();
            }
            mask.SetBit(tagIndex);
        }

        
        public void UnsetTag(int tagIndex)
        {
            mask?.ClearBit(tagIndex);
        }

        
        public bool HasTag(int tagIndex)
        {
            return mask?.IsSet(tagIndex) ?? false;
        }
    }
}

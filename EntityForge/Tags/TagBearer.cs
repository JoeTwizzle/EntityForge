using EntityForge.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace EntityForge.Tags
{
    internal struct TagBearer : IComponent<TagBearer>
    {
        internal BitMask mask;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetTag(int tagIndex)
        {
            if (mask is null)
            {
                mask = new BitMask();
            }
            mask.SetBit(tagIndex);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UnsetTag(int tagIndex)
        {
            mask?.ClearBit(tagIndex);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasTag(int tagIndex)
        {
            return mask?.IsSet(tagIndex) ?? false;
        }
    }
}

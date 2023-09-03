using EntityForge.Helpers;
using EntityForge.Tags;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace EntityForge
{
    public sealed partial class World
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasTag<T>(EntityId entity) where T : struct, ITag<T>
        {
            ref var tag = ref GetComponentOrNullRef<TagBearer>(entity);
            int tagIndex = GetOrCreateTagId<T>();
            return !Unsafe.IsNullRef(ref tag) && tag.HasTag(tagIndex);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddTag<T>(EntityId entity) where T : struct, ITag<T>
        {
            ref var tag = ref SetComponent<TagBearer>(entity);
            int tagIndex = GetOrCreateTagId<T>();
            if (tag.HasTag(tagIndex))
            {
                ThrowHelper.ThrowArgumentException("Tag already present on entity");
            }
            tag.SetTag(tagIndex);
            InvokeTagAddEvent(entity, tagIndex);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetTag<T>(EntityId entity) where T : struct, ITag<T>
        {
            ref var tag = ref SetComponent<TagBearer>(entity);
            int tagIndex = GetOrCreateTagId<T>();
            if (!tag.HasTag(tagIndex))
            {
                tag.SetTag(tagIndex);
                InvokeTagAddEvent(entity, tagIndex);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UnsetTag<T>(EntityId entity) where T : struct, ITag<T>
        {
            ref var tag = ref GetComponentOrNullRef<TagBearer>(entity);
            int tagIndex = GetOrCreateTagId<T>();
            if (!Unsafe.IsNullRef(ref tag) && tag.HasTag(tagIndex))
            {
                InvokeTagRemoveEvent(entity, tagIndex);
                tag.UnsetTag(tagIndex);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveTag<T>(EntityId entity) where T : struct, ITag<T>
        {
            ref var tag = ref GetComponentOrNullRef<TagBearer>(entity);
            int tagIndex = GetOrCreateTagId<T>();
            if (Unsafe.IsNullRef(ref tag) || !tag.HasTag(tagIndex))
            {
                ThrowHelper.ThrowArgumentException("Tag is not present on entity");
            }
            else
            {
                tag.UnsetTag(tagIndex);
                InvokeTagRemoveEvent(entity, tagIndex);
            }
        }
    }
}

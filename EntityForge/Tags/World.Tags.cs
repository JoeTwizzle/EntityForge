using EntityForge.Helpers;
using EntityForge.Tags;
using System.Runtime.CompilerServices;

namespace EntityForge
{
    public sealed partial class World
    {
        
        public bool HasTag<T>(EntityId entity) where T : struct, ITag<T>
        {
            ref var tag = ref GetComponentOrNullRef<TagBearer>(entity);
            int tagIndex = GetOrCreateTagId<T>();
            return !Unsafe.IsNullRef(ref tag) && tag.HasTag(tagIndex);
        }

        
        public void AddTag<T>(EntityId entity) where T : struct, ITag<T>
        {
            ref var tag = ref SetComponent<TagBearer>(entity);
            int tagIndex = GetOrCreateTagId<T>();
            if (tag.HasTag(tagIndex))
            {
                ThrowHelper.ThrowArgumentException("Tag already present on entity");
            }
            var arch = GetArchetype(entity);
            if (arch.IsLocked)
            {
                arch.commandBuffer.AddTag(entity, tagIndex);
                return;
            }
            tag.SetTag(tagIndex);
            InvokeTagAddEvent(entity, tagIndex);
        }

        
        public void SetTag<T>(EntityId entity) where T : struct, ITag<T>
        {
            ref var tag = ref SetComponent<TagBearer>(entity);
            int tagIndex = GetOrCreateTagId<T>();
            if (!tag.HasTag(tagIndex))
            {
                var arch = GetArchetype(entity);
                if (arch.IsLocked)
                {
                    arch.commandBuffer.AddTag(entity, tagIndex);
                    return;
                }
                tag.SetTag(tagIndex);
                InvokeTagAddEvent(entity, tagIndex);
            }
        }

        
        public void UnsetTag<T>(EntityId entity) where T : struct, ITag<T>
        {
            ref var tag = ref GetComponentOrNullRef<TagBearer>(entity);
            int tagIndex = GetOrCreateTagId<T>();
            if (!Unsafe.IsNullRef(ref tag) && tag.HasTag(tagIndex))
            {
                var arch = GetArchetype(entity);
                if (arch.IsLocked)
                {
                    arch.commandBuffer.RemoveTag(entity, tagIndex);
                    return;
                }
                InvokeTagRemoveEvent(entity, tagIndex);
                tag.UnsetTag(tagIndex);
            }
        }

        
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
                var arch = GetArchetype(entity);
                if (arch.IsLocked)
                {
                    arch.commandBuffer.RemoveTag(entity, tagIndex);
                    return;
                }
                tag.UnsetTag(tagIndex);
                InvokeTagRemoveEvent(entity, tagIndex);
            }
        }
    }
}

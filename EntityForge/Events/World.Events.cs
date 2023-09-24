using EntityForge.Collections;
using EntityForge.Events;
using EntityForge.Tags;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace EntityForge
{
    public sealed partial class World
    {

        private readonly BitMask componentEventsEnabledMask = new();
        private readonly BitMask tagEventsEnabledMask = new();
#pragma warning disable CA1003 // Use generic event handler instances
        public event EntityEvent? OnEntityCreated;
        public event EntityEvent? OnEntityDelete;
        public event ComponentEvent? OnComponentAdd;
        public event ComponentEvent? OnComponentRemove;
        public event TagEvent? OnTagAdd;
        public event TagEvent? OnTagRemove;
#pragma warning restore CA1003 // Use generic event handler instances
        private readonly Dictionary<int, List<ComponentEvent>> _componentAddEvents = new();
        private readonly Dictionary<int, List<TagEvent>> _tagAddEvents = new();
        private readonly Dictionary<int, List<ComponentEvent>> _componentRemoveEvents = new();
        private readonly Dictionary<int, List<TagEvent>> _tagRemoveEvents = new();

        public bool EntityEventsEnabled { get; set; }
        public bool GlobalComponentEventsEnabled { get; set; }
        public bool GlobalTagEventsEnabled { get; set; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void EnableTagEvents<T>() where T : struct, ITag<T>
        {
            tagEventsEnabledMask.SetBit(GetOrCreateTagId<T>());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DisableTagEvents<T>() where T : struct, ITag<T>
        {
            tagEventsEnabledMask.ClearBit(GetOrCreateTagId<T>());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void EnableComponentEvents<T>() where T : struct, IComponent<T>
        {
            componentEventsEnabledMask.SetBit(GetOrCreateTypeId<T>());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DisableComponentEvents<T>() where T : struct, IComponent<T>
        {
            componentEventsEnabledMask.ClearBit(GetOrCreateTypeId<T>());
        }

        //Component
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SubscribeOnComponentAdd<T>(ComponentEvent componentEventHandler) where T : struct, IComponent<T>
        {
            ref var list = ref CollectionsMarshal.GetValueRefOrAddDefault(_componentAddEvents, GetOrCreateTypeId<T>(), out bool exists);
            if (list is null)
            {
                list = new();
            }
            list.Add(componentEventHandler);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UnsubscribeOnComponentAdd<T>(ComponentEvent componentEventHandler) where T : struct, IComponent<T>
        {
            ref var list = ref CollectionsMarshal.GetValueRefOrAddDefault(_componentAddEvents, GetOrCreateTypeId<T>(), out bool exists);
            if (list is not null)
            {
                list.Remove(componentEventHandler);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SubscribeOnComponentRemove<T>(ComponentEvent componentEventHandler) where T : struct, IComponent<T>
        {
            ref var list = ref CollectionsMarshal.GetValueRefOrAddDefault(_componentRemoveEvents, GetOrCreateTypeId<T>(), out bool exists);
            if (list is null)
            {
                list = new();
            }
            list.Add(componentEventHandler);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UnsubscribeOnComponentRemove<T>(ComponentEvent componentEventHandler) where T : struct, IComponent<T>
        {
            ref var list = ref CollectionsMarshal.GetValueRefOrAddDefault(_componentRemoveEvents, GetOrCreateTypeId<T>(), out bool exists);
            if (list is not null)
            {
                list.Remove(componentEventHandler);
            }
        }

        //Tag
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SubscribeOnTagAdd<T>(TagEvent componentEventHandler) where T : struct, ITag<T>
        {
            ref var list = ref CollectionsMarshal.GetValueRefOrAddDefault(_tagAddEvents, GetOrCreateTagId<T>(), out bool exists);
            if (list is null)
            {
                list = new();
            }
            list.Add(componentEventHandler);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UnsubscribeOnTagAdd<T>(TagEvent componentEventHandler) where T : struct, ITag<T>
        {
            ref var list = ref CollectionsMarshal.GetValueRefOrAddDefault(_tagAddEvents, GetOrCreateTagId<T>(), out bool exists);
            if (list is not null)
            {
                list.Remove(componentEventHandler);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SubscribeOnTagRemove<T>(TagEvent componentEventHandler) where T : struct, ITag<T>
        {
            ref var list = ref CollectionsMarshal.GetValueRefOrAddDefault(_tagRemoveEvents, GetOrCreateTagId<T>(), out bool exists);
            if (list is null)
            {
                list = new();
            }
            list.Add(componentEventHandler);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UnsubscribeOnTagRemove<T>(TagEvent componentEventHandler) where T : struct, ITag<T>
        {
            ref var list = ref CollectionsMarshal.GetValueRefOrAddDefault(_tagRemoveEvents, GetOrCreateTagId<T>(), out bool exists);
            if (list is not null)
            {
                list.Remove(componentEventHandler);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void InvokeComponentAddEvent(EntityId entity, int componentId)
        {
            if (GlobalComponentEventsEnabled)
            {
                OnComponentAdd?.Invoke(entity, componentId);
            }
            if (componentEventsEnabledMask.IsSet(componentId) && _componentAddEvents.TryGetValue(componentId, out var list))
            {
                foreach (var componentEvent in list)
                {
                    componentEvent.Invoke(entity, componentId);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void InvokeComponentRemoveEvent(EntityId entity, int componentId)
        {
            if (GlobalComponentEventsEnabled)
            {
                OnComponentRemove?.Invoke(entity, componentId);
            }
            if (componentEventsEnabledMask.IsSet(componentId) && _componentRemoveEvents.TryGetValue(componentId, out var list))
            {
                foreach (var componentEvent in list)
                {
                    componentEvent.Invoke(entity, componentId);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void InvokeTagAddEvent(EntityId entity, int tagId)
        {
            if (GlobalTagEventsEnabled)
            {
                OnTagAdd?.Invoke(entity, tagId);
            }
            if (tagEventsEnabledMask.IsSet(tagId) && _tagAddEvents.TryGetValue(tagId, out var list))
            {
                foreach (var tagEvent in list)
                {
                    tagEvent.Invoke(entity, tagId);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void InvokeTagRemoveEvent(EntityId entity, int tagId)
        {
            if (GlobalTagEventsEnabled)
            {
                OnTagRemove?.Invoke(entity, tagId);
            }
            if (tagEventsEnabledMask.IsSet(tagId) && _tagRemoveEvents.TryGetValue(tagId, out var list))
            {
                foreach (var tagEvent in list)
                {
                    tagEvent.Invoke(entity, tagId);
                }
            }
        }

    }
}

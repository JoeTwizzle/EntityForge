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

        
        public void EnableTagEvents<T>() where T : struct, ITag<T>
        {
            tagEventsEnabledMask.SetBit(GetOrCreateTagId<T>());
        }

        
        public void DisableTagEvents<T>() where T : struct, ITag<T>
        {
            tagEventsEnabledMask.ClearBit(GetOrCreateTagId<T>());
        }

        
        public void EnableComponentEvents<T>() where T : struct, IComponent<T>
        {
            componentEventsEnabledMask.SetBit(GetOrCreateTypeId<T>());
        }

        
        public void DisableComponentEvents<T>() where T : struct, IComponent<T>
        {
            componentEventsEnabledMask.ClearBit(GetOrCreateTypeId<T>());
        }

        //Component
        
        public void SubscribeOnComponentAdd<T>(ComponentEvent componentEventHandler) where T : struct, IComponent<T>
        {
            ref var list = ref CollectionsMarshal.GetValueRefOrAddDefault(_componentAddEvents, GetOrCreateTypeId<T>(), out bool exists);
            if (list is null)
            {
                list = new();
            }
            list.Add(componentEventHandler);
        }

        
        public void UnsubscribeOnComponentAdd<T>(ComponentEvent componentEventHandler) where T : struct, IComponent<T>
        {
            ref var list = ref CollectionsMarshal.GetValueRefOrAddDefault(_componentAddEvents, GetOrCreateTypeId<T>(), out bool exists);
            if (list is not null)
            {
                list.Remove(componentEventHandler);
            }
        }

        
        public void SubscribeOnComponentRemove<T>(ComponentEvent componentEventHandler) where T : struct, IComponent<T>
        {
            ref var list = ref CollectionsMarshal.GetValueRefOrAddDefault(_componentRemoveEvents, GetOrCreateTypeId<T>(), out bool exists);
            if (list is null)
            {
                list = new();
            }
            list.Add(componentEventHandler);
        }

        
        public void UnsubscribeOnComponentRemove<T>(ComponentEvent componentEventHandler) where T : struct, IComponent<T>
        {
            ref var list = ref CollectionsMarshal.GetValueRefOrAddDefault(_componentRemoveEvents, GetOrCreateTypeId<T>(), out bool exists);
            if (list is not null)
            {
                list.Remove(componentEventHandler);
            }
        }

        //Tag
        
        public void SubscribeOnTagAdd<T>(TagEvent componentEventHandler) where T : struct, ITag<T>
        {
            ref var list = ref CollectionsMarshal.GetValueRefOrAddDefault(_tagAddEvents, GetOrCreateTagId<T>(), out bool exists);
            if (list is null)
            {
                list = new();
            }
            list.Add(componentEventHandler);
        }

        
        public void UnsubscribeOnTagAdd<T>(TagEvent componentEventHandler) where T : struct, ITag<T>
        {
            ref var list = ref CollectionsMarshal.GetValueRefOrAddDefault(_tagAddEvents, GetOrCreateTagId<T>(), out bool exists);
            if (list is not null)
            {
                list.Remove(componentEventHandler);
            }
        }

        
        public void SubscribeOnTagRemove<T>(TagEvent componentEventHandler) where T : struct, ITag<T>
        {
            ref var list = ref CollectionsMarshal.GetValueRefOrAddDefault(_tagRemoveEvents, GetOrCreateTagId<T>(), out bool exists);
            if (list is null)
            {
                list = new();
            }
            list.Add(componentEventHandler);
        }

        
        public void UnsubscribeOnTagRemove<T>(TagEvent componentEventHandler) where T : struct, ITag<T>
        {
            ref var list = ref CollectionsMarshal.GetValueRefOrAddDefault(_tagRemoveEvents, GetOrCreateTagId<T>(), out bool exists);
            if (list is not null)
            {
                list.Remove(componentEventHandler);
            }
        }

        
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

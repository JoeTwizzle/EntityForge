using CommunityToolkit.HighPerformance;
using EntityForge.Collections;
using EntityForge.Tags;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace EntityForge;

public delegate void EntityEvent(EntityId entityId);
public delegate void ComponentEvent(EntityId entityId, int componentId);
public delegate void TagEvent(EntityId entityId, int tagId);

public sealed partial class World
{
    public bool GlobalEntityEventsEnabled { get; set; }
    public bool GlobalComponentEventsEnabled { get; set; }
    public bool GlobalTagEventsEnabled { get; set; }

#pragma warning disable CA1003 // Use generic event handler instances
    public event EntityEvent? OnEntityCreated;
    public event EntityEvent? OnEntityDelete;
    public event ComponentEvent? OnComponentAdd;
    public event ComponentEvent? OnComponentRemove;
    public event TagEvent? OnTagAdd;
    public event TagEvent? OnTagRemove;
#pragma warning restore CA1003 // Use generic event handler instances

    private readonly BitMask _componentEventsEnabledMask = new();
    private readonly BitMask _tagEventsEnabledMask = new();
    private readonly Dictionary<int, List<ComponentEvent>> _componentAddEvents = new();
    private readonly Dictionary<int, List<TagEvent>> _tagAddEvents = new();
    private readonly Dictionary<int, List<ComponentEvent>> _componentRemoveEvents = new();
    private readonly Dictionary<int, List<TagEvent>> _tagRemoveEvents = new();

    public void EnableTagEvents<T>() where T : struct, ITag<T>
    {
        _tagEventsEnabledMask.SetBit(GetOrCreateTagId<T>());
    }

    public void DisableTagEvents<T>() where T : struct, ITag<T>
    {
        _tagEventsEnabledMask.ClearBit(GetOrCreateTagId<T>());
    }

    public void EnableComponentEvents<T>() where T : struct, IComponent<T>
    {
        _componentEventsEnabledMask.SetBit(GetOrCreateComponentId<T>());
    }

    public void DisableComponentEvents<T>() where T : struct, IComponent<T>
    {
        _componentEventsEnabledMask.ClearBit(GetOrCreateComponentId<T>());
    }

    //Component
    private static List<ComponentEvent>? GetComponentList<T>(Dictionary<int, List<ComponentEvent>> eventDict) where T : struct, IComponent<T>
    {
        ref var list = ref CollectionsMarshal.GetValueRefOrAddDefault(eventDict, GetOrCreateComponentId<T>(), out bool exists);
        if (!exists)
        {
            return null;
        }
        return list;
    }

    private static List<ComponentEvent> GetOrCreateComponentList<T>(Dictionary<int, List<ComponentEvent>> eventDict) where T : struct, IComponent<T>
    {
        ref var list = ref CollectionsMarshal.GetValueRefOrAddDefault(eventDict, GetOrCreateComponentId<T>(), out bool exists);
        if (!exists)
        {
            list = new();
        }
        return list!;
    }

    public void SubscribeOnComponentAdd<T>(ComponentEvent componentEventHandler) where T : struct, IComponent<T>
    {
        GetOrCreateComponentList<T>(_componentAddEvents).Add(componentEventHandler);
    }

    public void UnsubscribeOnComponentAdd<T>(ComponentEvent componentEventHandler) where T : struct, IComponent<T>
    {
        GetComponentList<T>(_componentAddEvents)?.Remove(componentEventHandler);
    }

    public void SubscribeOnComponentRemove<T>(ComponentEvent componentEventHandler) where T : struct, IComponent<T>
    {
        GetOrCreateComponentList<T>(_componentRemoveEvents).Add(componentEventHandler);
    }

    public void UnsubscribeOnComponentRemove<T>(ComponentEvent componentEventHandler) where T : struct, IComponent<T>
    {
        GetComponentList<T>(_componentRemoveEvents)?.Remove(componentEventHandler);
    }

    //Tag
    private static List<TagEvent>? GetTagList<T>(Dictionary<int, List<TagEvent>> eventDict) where T : struct, ITag<T>
    {
        ref var list = ref CollectionsMarshal.GetValueRefOrAddDefault(eventDict, GetOrCreateTagId<T>(), out bool exists);
        if (exists)
        {
            return list;
        }
        return null;
    }

    private static List<TagEvent> GetOrCreateTagList<T>(Dictionary<int, List<TagEvent>> eventDict) where T : struct, ITag<T>
    {
        ref var list = ref CollectionsMarshal.GetValueRefOrAddDefault(eventDict, GetOrCreateTagId<T>(), out bool exists);
        if (list is null)
        {
            list = new();
        }
        return list;
    }

    public void SubscribeOnTagAdd<T>(TagEvent componentEventHandler) where T : struct, ITag<T>
    {
        GetOrCreateTagList<T>(_tagAddEvents).Add(componentEventHandler);
    }

    public void SubscribeOnTagRemove<T>(TagEvent componentEventHandler) where T : struct, ITag<T>
    {
        GetOrCreateTagList<T>(_tagRemoveEvents).Add(componentEventHandler);
    }

    public void UnsubscribeOnTagAdd<T>(TagEvent componentEventHandler) where T : struct, ITag<T>
    {
        GetTagList<T>(_tagAddEvents)?.Remove(componentEventHandler);
    }

    public void UnsubscribeOnTagRemove<T>(TagEvent componentEventHandler) where T : struct, ITag<T>
    {
        GetTagList<T>(_tagRemoveEvents)?.Remove(componentEventHandler);
    }

    internal void InvokeCreateEntityEvent(EntityId entityId)
    {
        if (GlobalEntityEventsEnabled)
        {
            OnEntityCreated?.Invoke(entityId);
        }
    }

    internal void InvokeDeleteEntityEvent(EntityId entityId)
    {
        if (GlobalEntityEventsEnabled)
        {
            OnEntityDelete?.Invoke(entityId);
        }
    }

    internal void InvokeComponentAddEvent(EntityId entity, int componentId)
    {
        if (GlobalComponentEventsEnabled)
        {
            OnComponentAdd?.Invoke(entity, componentId);
        }
        if (_componentEventsEnabledMask.IsSet(componentId) && _componentAddEvents.TryGetValue(componentId, out var list))
        {
            foreach (var componentEvent in list.AsSpan())
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
        if (_componentEventsEnabledMask.IsSet(componentId) && _componentRemoveEvents.TryGetValue(componentId, out var list))
        {
            foreach (var componentEvent in list.AsSpan())
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
        if (_tagEventsEnabledMask.IsSet(tagId) && _tagAddEvents.TryGetValue(tagId, out var list))
        {
            foreach (var tagEvent in list.AsSpan())
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
        if (_tagEventsEnabledMask.IsSet(tagId) && _tagRemoveEvents.TryGetValue(tagId, out var list))
        {
            foreach (var tagEvent in list.AsSpan())
            {
                tagEvent.Invoke(entity, tagId);
            }
        }
    }
}

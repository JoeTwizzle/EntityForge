using EntityForge.Collections;
using EntityForge.Collections.Generic;
using EntityForge.Helpers;
using EntityForge.Tags;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace EntityForge;

public sealed partial class World
{
    internal void AddComponentInternal(EntityId entity, ComponentInfo info, Archetype arch)
    {
        if (arch.IsLocked)
        {
            arch.commandBuffer.Add(entity, info);
        }
        else
        {
            ValidateAddDebug(arch, info.TypeId);
            var newArch = GetOrCreateArchetypeVariantAdd(arch, info);
            MoveEntity(arch, newArch, entity);
            InvokeComponentAddEvent(entity, info.TypeId);
        }
    }

    internal void AddComponentWithValueInternal<T>(EntityId entity, T value, Archetype arch) where T : struct, IComponent<T>
    {
        var info = GetOrCreateComponentInfo<T>();
        if (arch.IsLocked)
        {
            arch.commandBuffer.AddWithValue(entity, value);
        }
        else
        {
            var newArch = GetOrCreateArchetypeVariantAdd(arch, info);
            var index = MoveEntity(arch, newArch, entity);
            var typeIndexRecord = GetTypeIndexRecord(newArch, info.TypeId).ComponentTypeIndex;
            ref T data = ref newArch.GetComponentByIndex<T>(index, typeIndexRecord);
            data = value;
            InvokeComponentAddEvent(entity, info.TypeId);
        }
    }

    private void RemoveComponentInternal(EntityId entity, Archetype arch, ComponentInfo info)
    {
        if (arch.IsLocked)
        {
            arch.commandBuffer.Remove(entity, info);
        }
        else
        {
            ValidateRemoveDebug(arch, info.TypeId);
            var newArch = GetOrCreateArchetypeVariantRemove(arch, info.TypeId);
            InvokeComponentRemoveEvent(entity, info.TypeId);
            MoveEntity(arch, newArch, entity);
        }
    }

    internal void SetValues(EntityId entity, UnsafeSparseSet<UnsafeSparseSet> valuesSetSet)
    {
        ref var entityIndex = ref GetEntityIndexRecord(entity);
        var archetype = entityIndex.Archetype;

        var valueSets = valuesSetSet.GetDenseData();
        var valueIndices = valuesSetSet.GetIndexData();
        var infos = archetype.componentInfo.Span;
        for (int i = 0; i < valueSets.Length; i++)
        {
            var typeId = valueIndices[i];
            var setBundle = valueSets[i];
            if (archetype.componentIdsMap.TryGetValue(typeId, out var index))
            {
                if (setBundle.TryGetIndex(entityIndex.ArchetypeColumn, out var denseIndex))
                {
                    var compInfo = infos[index];
                    //copy from components to archetypes components
                    if (compInfo.IsUnmanaged)
                    {
                        unsafe
                        {
                            setBundle.CopyToUnmanagedRaw(denseIndex, archetype.componentPools[index].UnmanagedData, entityIndex.ArchetypeColumn, compInfo.UnmanagedSize);
                        }
                    }
                    else
                    {
                        setBundle.CopyToManagedRaw(denseIndex, archetype.componentPools[index].ManagedData!, entityIndex.ArchetypeColumn, 1);
                    }
                }
            }
        }
    }

    public bool HasComponent(EntityId entity, int component)
    {
        ValidateAliveDebug(entity);
        ref EntityIndexRecord record = ref GetEntityIndexRecord(entity);
        Archetype archetype = record.Archetype;

        if (_typeIndexMap.TryGetValue(component, out var archetypes))
        {
            return archetypes.ContainsKey(archetype.Index);
        }
        return false;
    }

    public void AddComponent(EntityId entity, ComponentInfo compInfo)
    {
        ValidateAliveDebug(entity);
        var arch = GetArchetype(entity);
        AddComponentInternal(entity, compInfo, arch);
    }

    public void RemoveComponent(EntityId entity, ComponentInfo typeId)
    {
        ValidateAliveDebug(entity);
        var arch = GetArchetype(entity);
        RemoveComponentInternal(entity, arch, typeId);
    }

    public bool SetComponent(EntityId entity, ComponentInfo compInfo)
    {
        ref var compIndexRecord = ref GetEntityIndexRecord(entity);
        var arch = compIndexRecord.Archetype;

        if (!HasComponent(entity, compInfo.TypeId))
        {
            AddComponentInternal(entity, compInfo, arch);
            return true;
        }
        return false;
    }

    public bool UnsetComponent(EntityId entity, Type component)
    {
        if (s_TypeMap.TryGetValue(component, out var meta))
        {
            return UnsetComponent(entity, GetComponentInfo(component));
        }
        return false;
    }

    public bool UnsetComponent(EntityId entity, ComponentInfo info)
    {
        ValidateAliveDebug(entity);
        var arch = GetArchetype(entity);
        if (HasComponent(entity, info.TypeId))
        {
            RemoveComponent(entity, info);
            return true;
        }
        return false;
    }

    public bool SetComponent<T>(EntityId entity, T value) where T : struct, IComponent<T>
    {
        ValidateAliveDebug(entity);
        ref var compIndexRecord = ref GetEntityIndexRecord(entity);
        var arch = compIndexRecord.Archetype;
        var compInfo = World.GetOrCreateComponentInfo<T>();
        if (HasComponent<T>(entity))
        {
            ref T data = ref arch.GetComponent<T>(compIndexRecord.ArchetypeColumn);
            data = value;
            return false;
        }
        else
        {
            AddComponentWithValueInternal(entity, value, arch);
            return true;
        }
    }

    public bool UnsetComponent<T>(EntityId entity) where T : struct, IComponent<T>
    {
        ValidateAliveDebug(entity);
        var arch = GetArchetype(entity);
        var typeId = World.GetOrCreateComponentId<T>();
        ref EntityIndexRecord record = ref GetEntityIndexRecord(entity);
        if (record.Archetype.TryGetComponentIndex<T>(out int index))
        {
            RemoveComponent<T>(entity);
            return true;
        }
        return false;
    }

    public void AddComponent<T>(EntityId entity) where T : struct, IComponent<T>
    {
        ValidateAliveDebug(entity);
        ref var record = ref GetEntityIndexRecord(entity);
        var compInfo = World.GetOrCreateComponentInfo<T>();
        AddComponentInternal(entity, compInfo, record.Archetype);
    }

    public void AddComponent<T>(EntityId entity, T value) where T : struct, IComponent<T>
    {
        ValidateAliveDebug(entity);
        var arch = GetArchetype(entity);
        var compInfo = World.GetOrCreateComponentInfo<T>();
        AddComponentWithValueInternal(entity, value, arch);
    }

    public void RemoveComponent<T>(EntityId entity) where T : struct, IComponent<T>
    {
        ValidateAliveDebug(entity);
        ref var record = ref GetEntityIndexRecord(entity);
        var compInfo = World.GetOrCreateComponentInfo<T>();
        RemoveComponentInternal(entity, record.Archetype, compInfo);
    }

    public bool HasComponent<T>(EntityId entity) where T : struct, IComponent<T>
    {
        ValidateAliveDebug(entity);
        ref EntityIndexRecord record = ref GetEntityIndexRecord(entity);
        if (record.Archetype.IsLocked)
        {
            return record.Archetype.commandBuffer.HasComponent(entity, GetOrCreateComponentId<T>());
        }
        return record.Archetype.HasComponent(GetOrCreateComponentId<T>());
    }

    public ref T GetComponent<T>(EntityId entity) where T : struct, IComponent<T>
    {
        ValidateAliveDebug(entity);
        // First check if archetype has id
        ref EntityIndexRecord record = ref GetEntityIndexRecord(entity);
        var typeId = GetOrCreateComponentId<T>();
        if (record.Archetype.IsLocked)
        {
            return ref record.Archetype.commandBuffer.GetComponent<T>(entity);
        }
        ValidateHasDebug(record.Archetype, typeId);
        return ref record.Archetype.GetComponent<T>(record.ArchetypeColumn, typeId);
    }

    public ref T GetComponentOrNullRef<T>(EntityId entity) where T : struct, IComponent<T>
    {
        ValidateAliveDebug(entity);
        // First check if archetype has id
        ref EntityIndexRecord record = ref GetEntityIndexRecord(entity);
        int typeId = GetOrCreateComponentId<T>();
        Archetype archetype = record.Archetype;
        if (record.Archetype.HasComponent(typeId))
        {
            return ref record.Archetype.GetComponent<T>(record.ArchetypeColumn, typeId);
        }
        if (record.Archetype.IsLocked)
        {
            return ref record.Archetype.commandBuffer.GetComponentOrNullRef<T>(entity);
        }
        return ref Unsafe.NullRef<T>();
    }

    public ref T SetComponent<T>(EntityId entity) where T : struct, IComponent<T>
    {
        ref var record = ref GetEntityIndexRecord(entity);
        var compInfo = World.GetOrCreateComponentInfo<T>();
        if (!record.Archetype.HasComponent(compInfo.TypeId))
        {
            AddComponentInternal(entity, compInfo, record.Archetype);
        }
        return ref record.Archetype.GetComponent<T>(record.ArchetypeColumn, compInfo.TypeId);
    }

    public void RemoveComponentFromAll<T>() where T : struct, IComponent<T>
    {
        var archetypes = GetContainingArchetypesWithType<T>();
        foreach (var item in archetypes)
        {
            var arch = GetArchetypeById(item.Key);
            var ents = arch.Entities;
            for (int i = ents.Length - 1; i >= 0; i--)
            {
                RemoveComponent<T>(ents[i]);
            }
        }
    }
}

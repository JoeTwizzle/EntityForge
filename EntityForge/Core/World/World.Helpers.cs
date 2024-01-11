using EntityForge.Collections;
using EntityForge.Helpers;
using EntityForge.Tags;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace EntityForge;

public sealed partial class World
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref EntityIndexRecord GetEntityIndexRecord(in EntityId entity)
    {
        return ref _entityIndex[entity.Id];
    }

    public Dictionary<int, TypeIndexRecord> GetContainingArchetypesWithType<T>() where T : struct, IComponent<T>
    {
        return _typeIndexMap[GetOrCreateComponentId<T>()];
    }

    public Dictionary<int, TypeIndexRecord> GetContainingArchetypesWithIndex(int componentType)
    {
        return _typeIndexMap[componentType];
    }

    public bool TryGetContainingArchetypes(int componentType, [NotNullWhen(true)] out Dictionary<int, TypeIndexRecord>? result)
    {
        return _typeIndexMap.TryGetValue(componentType, out result);
    }

    public ref TypeIndexRecord GetTypeIndexRecord(Archetype archetype, int typeId)
    {
        return ref GetContainingArchetypesWithIndex(typeId).Get(archetype.Index);
    }

    [Conditional("DEBUG")]
    private static void ValidateHasDebug(Archetype archetype, int type)
    {
        if (!archetype.HasComponent(type))
        {
            ThrowHelper.ThrowMissingComponentException($"The entity does not have a Component with typeId {type}");
        }
    }

    [Conditional("DEBUG")]
    private static void ValidateAddDebug(Archetype archetype, int type)
    {
        if (archetype.HasComponent(type))
        {
            ThrowHelper.ThrowDuplicateComponentException($"Tried adding duplicate Component with typeId {type}");
        }
    }

    [Conditional("DEBUG")]
    private static void ValidateRemoveDebug(Archetype archetype, int type)
    {
        if (!archetype.HasComponent(type))
        {
            ThrowHelper.ThrowMissingComponentException($"Tried removing missing Component with typeId {type}");
        }
    }

    [Conditional("DEBUG")]
    private void ValidateAliveDebug(EntityId entity)
    {
        if (!IsAlive(entity))
        {
            ThrowHelper.ThrowArgumentException($"Tried accessing destroyed entityId: {entity}");
        }
    }

    [Conditional("DEBUG")]
    private void ValidateDestroyedDebug(EntityId entity)
    {
        if (IsAlive(entity))
        {
            ThrowHelper.ThrowArgumentException($"Tried accessing alive entityId: {entity}");
        }
    }
}

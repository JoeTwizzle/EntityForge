using EntityForge.Collections;
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
    /// <summary>
    /// Indexed by entity id <br/> Span of all indices for entities
    /// </summary>
    public ReadOnlySpan<EntityIndexRecord> EntityIndices => new ReadOnlySpan<EntityIndexRecord>(_entityIndex, 0, _entityCounter);
    /// <summary>
    /// Lock
    /// </summary>
    internal readonly ReaderWriterLockSlim worldEntitiesRWLock = new();
    /// <summary>
    /// Contains now deleted Entities whoose ids may be reused
    /// </summary>
    private EntityId[] _recycledEntities;
    /// <summary>
    /// Stores in which archetype an entityId is
    /// </summary>
    private EntityIndexRecord[] _entityIndex;

    public bool IsAlive(Entity entity)
    {
        return entity.EntityId.Id < _entityCounter && _entityIndex[entity.EntityId.Id].EntityVersion == entity.Version;
    }

    public bool IsAlive(EntityId entity)
    {
        return entity.Id < _entityCounter && _entityIndex[entity.Id].EntityVersion > 0;
    }

    public Archetype GetArchetype(EntityId entity)
    {
        return GetEntityIndexRecord(entity).Archetype;
    }

    public void ReserveEntities(in ArchetypeDefinition definition, int count)
    {
        var archetype = GetOrCreateArchetype(definition);
        if (archetype.IsLocked)
        {
            archetype.commandBuffer.Reserve(count);
            return;
        }
        archetype.GrowBy(count);
    }

    public Entity CreateEntity()
    {
        return CreateEntity(s_emptyArchetypeDefinition);
    }

    public Entity CreateEntity(in ArchetypeDefinition definition)
    {
        var archetype = GetOrCreateArchetype(definition);
        EntityId entityId;
        if (_recycledEntitiesCount > 0)
        {
            entityId = _recycledEntities[--_recycledEntitiesCount];
        }
        else
        {
            _entityIndex = _entityIndex.GrowIfNeeded(_entityCounter, 1);
            entityId = new EntityId(_entityCounter++);
            _entityIndex[entityId.Id].EntityVersion = -1;
        }
        ref var entIndex = ref _entityIndex[entityId.Id];
        entIndex.Archetype = archetype;
        entIndex.EntityVersion = (short)-entIndex.EntityVersion;
        entIndex.ArchetypeColumn = archetype.elementCount;
        var entity = new Entity(entityId.Id, entIndex.EntityVersion, WorldId);
        if (archetype.IsLocked)
        {
            entIndex.ArchetypeColumn = archetype.commandBuffer.Create(entityId);
            return entity;
        }
        archetype.AddEntityInternal(entity);
        InvokeCreateEntityEvent(entityId);
        return entity;
    }

    public void DeleteEntity(EntityId entityId)
    {
        ValidateAliveDebug(entityId);

        ref EntityIndexRecord compIndexRecord = ref GetEntityIndexRecord(entityId);
        var src = compIndexRecord.Archetype;
        //Get index of entityId to be removed
        ref var entityIndex = ref _entityIndex[entityId.Id];
        //Set its version to its negative increment (Mark entityId as destroyed)
        entityIndex.EntityVersion = (short)-(entityIndex.EntityVersion + 1);
        worldEntitiesRWLock.EnterWriteLock();
        _recycledEntities = _recycledEntities.GrowIfNeeded(_recycledEntitiesCount, 1);
        _recycledEntities[_recycledEntitiesCount++] = entityId;
        worldEntitiesRWLock.ExitWriteLock();
        if (src.IsLocked)
        {
            src.commandBuffer.Destroy(entityId);
            return;
        }

        DeleteEntityInternal(src, compIndexRecord.ArchetypeColumn);
        InvokeDeleteEntityEvent(entityId);
    }

    internal void DeleteEntityInternal(Archetype src, int oldIndex)
    {
        //Fill hole in id sparseArray
        src.FillHole(oldIndex);
        //Update index of entityId filling the hole
        ref EntityIndexRecord rec = ref GetEntityIndexRecord(src.Entities[--src.elementCount]);
        rec.ArchetypeColumn = oldIndex;
    }

    public Entity GetEntity(EntityId id)
    {
        return new Entity(id.Id, GetEntityIndexRecord(id).EntityVersion, WorldId);
    }

    internal int MoveEntity(Archetype src, Archetype dest, EntityId entity)
    {
        Debug.Assert(src.Index != dest.Index && !src.IsLocked);

        ref EntityIndexRecord compIndexRecord = ref GetEntityIndexRecord(entity);
        int oldIndex = compIndexRecord.ArchetypeColumn;
        dest.GrowBy(1);
        dest.entitiesPool.GetRefAt(dest.elementCount) = new Entity(entity.Id, compIndexRecord.EntityVersion, WorldId);
        int newIndex = dest.elementCount++;
        //Copy Pool to new Arrays
        src.CopyComponents(oldIndex, dest, newIndex);
        //Fill hole in old Arrays
        src.FillHole(oldIndex);

        //Update index of entityId filling the hole
        ref EntityIndexRecord rec = ref GetEntityIndexRecord(src.Entities[src.elementCount - 1]);
        rec.ArchetypeColumn = oldIndex;
        //Update index of moved entityId
        compIndexRecord.ArchetypeColumn = newIndex;
        compIndexRecord.Archetype = dest;
        //Finish removing entityId from source
        src.elementCount--;
        return newIndex;
    }
}

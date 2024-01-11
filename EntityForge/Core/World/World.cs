using EntityForge.Collections;
using EntityForge.Collections.Generic;
using EntityForge.Core;
using EntityForge.Helpers;
using EntityForge.Queries;
using EntityForge.Tags;
using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace EntityForge;

public sealed partial class World : IDisposable
{
    public static ReadOnlySpan<World> Worlds => new ReadOnlySpan<World>(s_worlds, 0, s_worldCounter);
    /// <summary>
    /// Unique Idenetifer of this world
    /// </summary>
    public short WorldId { get; private init; }
    public bool Disposed { get; private set; }
    private static readonly object s_createWorldLock = new object();
    private static readonly List<short> s_recycledWorlds = new();
    private static World[] s_worlds = new World[1];
    private static short s_worldCounter;
    int _filterCount;
    int _archetypeCount;
    int _entityCounter;
    int _recycledEntitiesCount;

    public World()
    {
        lock (s_createWorldLock)
        {
            _archetypes = new Archetype[DefaultComponents];
            _filters = new ArchetypeFilter[DefaultComponents];
            _filterMap = new(DefaultComponents);
            _archetypeIndexMap = new(DefaultComponents);
            _typeIndexMap = new(DefaultComponents);
            _entityIndex = new EntityIndexRecord[DefaultEntities];
            _recycledEntities = new EntityId[4];

            WorldId = GetNextWorldId();
            s_worlds = s_worlds.GrowIfNeeded(s_worldCounter, 1);
            s_worlds[WorldId] = this;
            //Create entityId with meta 0 and mark it as Destroyed
            //We do this so default(entityId) is not a valid entityId
            ref var idx = ref _entityIndex[CreateEntity().EntityId.Id];
            idx.EntityVersion = (short)-1;
            idx.Archetype.elementCount--;
        }
    }

    private static short GetNextWorldId()
    {
        if (s_recycledWorlds.Count > 0)
        {
            int lastIdx = s_recycledWorlds.Count - 1;
            short id = s_recycledWorlds[lastIdx];
            s_recycledWorlds.RemoveAt(lastIdx);
            return id;
        }
        return s_worldCounter++;
    }

    public void Dispose()
    {
        Disposed = true;
        Reset();
        s_worlds[WorldId] = null!;
        worldEntitiesRWLock.Dispose();
        worldFilterRWLock.Dispose();
        worldArchetypesRWLock.Dispose();
        s_recycledWorlds.Add(WorldId);
    }

    public void Reset()
    {
        worldArchetypesRWLock.EnterWriteLock();
        worldFilterRWLock.EnterWriteLock();
        worldEntitiesRWLock.EnterWriteLock();
        _filterMap.Clear();
        _typeIndexMap.Clear();
        _archetypeIndexMap.Clear();
        _archetypes.AsSpan(0, _archetypeCount).Clear();
        _filters.AsSpan(0, _filterCount).Clear();
        _entityIndex.AsSpan(0, _entityCounter).Clear();
        _recycledEntities.AsSpan(0, _recycledEntitiesCount).Clear();
        _entityCounter = 0;
        _archetypeCount = 0;
        _filterCount = 0;
        worldEntitiesRWLock.ExitWriteLock();
        worldFilterRWLock.ExitWriteLock();
        worldArchetypesRWLock.ExitWriteLock();
    }

    public static void DestroyAllWorlds()
    {
        lock (s_createWorldLock)
        {
            s_worldCounter = 0;
            for (int i = 0; i < s_worldCounter; i++)
            {
                if (!s_worlds[i].Disposed)
                {
                    s_worlds[i].Dispose();
                }
                s_worlds[i] = null!;
            }
        }
    }
}

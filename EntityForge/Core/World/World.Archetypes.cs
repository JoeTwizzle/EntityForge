using EntityForge.Collections;
using EntityForge.Helpers;
using EntityForge.Tags;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace EntityForge;

public sealed partial class World
{
    public const int DefaultComponents = 16;
    public const int DefaultEntities = 256;
    internal static readonly ArchetypeDefinition s_emptyArchetypeDefinition = 
        new(ComponentInfo.GetComponentHash([]), Array.Empty<ComponentInfo>());
    /// <summary>
    /// Number of different archtypes in this World
    /// </summary>
    public int ArchetypeCount => _archetypeCount;
    /// <summary>
    /// Span of all variantMap currently present in the World
    /// </summary>
    public ReadOnlySpan<Archetype> Archetypes => new ReadOnlySpan<Archetype>(_archetypes, 0, _archetypeCount);
    /// <summary>
    /// lock
    /// </summary>
    internal readonly ReaderWriterLockSlim worldArchetypesRWLock = new();
    /// <summary>
    /// Array of all archetypes present in this world
    /// </summary>
    private Archetype[] _archetypes;
    /// <summary>
    /// Maps an Archetype's Definition to its index
    /// </summary>
    private readonly Dictionary<ArchetypeDefinition, int> _archetypeIndexMap;
    /// <summary>
    /// componentID, archetype.Index -> archetype.ComponentTypeIndex
    /// </summary>
    private readonly Dictionary<int, Dictionary<int, TypeIndexRecord>> _typeIndexMap;

    public Archetype GetArchetypeById(int id)
    {
        worldArchetypesRWLock.EnterReadLock();
        var arch = _archetypes[id];
        worldArchetypesRWLock.ExitReadLock();
        return arch;
    }

    public Archetype GetOrCreateArchetype(in ArchetypeDefinition definition)
    {
        return GetArchetype(definition) ?? CreateArchetype(definition);
    }

    public Archetype? GetArchetype(in ArchetypeDefinition definition)
    {
        worldArchetypesRWLock.EnterReadLock();
        if (_archetypeIndexMap.TryGetValue(definition, out var index))
        {
            var arch = _archetypes[index];
            worldArchetypesRWLock.ExitReadLock();
            return arch;
        }
        worldArchetypesRWLock.ExitReadLock();
        return null;
    }

    internal Archetype GetOrCreateArchetypeVariantAdd(Archetype source, ComponentInfo compInfo)
    {
        //Archetype already stored in graph
        if (source.TryGetSiblingAdd(compInfo.TypeId, out Archetype? archetype))
        {
            return archetype;
        }
        int length = source.componentInfo.Length + 1;
        ComponentInfo[] pool = ArrayPool<ComponentInfo>.Shared.Rent(length);
        var infos = source.componentInfo.Span;
        for (int i = 0; i < source.componentInfo.Length; i++)
        {
            pool[i] = infos[i];
        }
        pool[length - 1] = compInfo;
        var memory = pool.AsMemory(0, length);
        int hash = ComponentInfo.GetComponentHash(memory.Span);
        archetype = GetArchetype(new ArchetypeDefinition(hash, memory));
        //Archetype does not yet exist, create it!
        if (archetype == null)
        {
            var definition = new ArchetypeDefinition(hash, memory.ToArray());
            archetype = CreateArchetype(definition);
        }
        ArrayPool<ComponentInfo>.Shared.Return(pool, true);
        source.SetSiblingAdd(compInfo.TypeId, archetype);
        return archetype;
    }


    internal Archetype GetOrCreateArchetypeVariantRemove(Archetype source, int compInfo)
    {
        //Archetype already stored in graph
        if (source.TryGetSiblingRemove(compInfo, out Archetype? archetype))
        {
            return archetype;
        }
        //Graph failed we need to find Archetype by hash
        int length = source.componentInfo.Length - 1;
        ComponentInfo[] pool = ArrayPool<ComponentInfo>.Shared.Rent(length);
        int index = 0;
        var infos = source.componentInfo.Span;
        for (int i = 0; i < source.componentInfo.Length; i++)
        {
            var compPool = infos[i];
            if (compPool.TypeId != compInfo)
            {
                pool[index++] = compPool;
            }
        }
        var memory = pool.AsMemory(0, length);
        int hash = ComponentInfo.GetComponentHash(memory.Span);
        archetype = GetArchetype(new ArchetypeDefinition(hash, memory));
        //Archetype does not yet exist, create it!
        if (archetype == null)
        {
            var definition = new ArchetypeDefinition(hash, memory.ToArray());
            archetype = CreateArchetype(definition);
        }
        ArrayPool<ComponentInfo>.Shared.Return(pool, true);
        source.SetSiblingRemove(compInfo, archetype);
        return archetype;
    }

    private Archetype CreateArchetype(in ArchetypeDefinition definition)
    {
        //Store type Definitions
        var mask = new BitMask();
        var infos = definition.ComponentInfos.Span;
        for (int i = 0; i < definition.ComponentInfos.Length; i++)
        {
            int id = infos[i].TypeId;
            mask.SetBit(id);
        }
        var archetype = new Archetype(this, definition.ComponentInfos, mask, definition.HashCode, _archetypeCount);
        worldArchetypesRWLock.EnterWriteLock();
        // Store in index
        for (int i = 0; i < infos.Length; i++)
        {
            ref var dict = ref CollectionsMarshal.GetValueRefOrAddDefault(_typeIndexMap, infos[i].TypeId, out var exists);
            if (!exists)
            {
                dict = new Dictionary<int, TypeIndexRecord>();
            }
            dict!.Add(archetype.Index, new TypeIndexRecord(i));
        }
        // Store in all variantMap
        _archetypeIndexMap.Add(definition, _archetypeCount);
        _archetypes = _archetypes.GrowIfNeeded(_archetypeCount, 1);
        _archetypes[_archetypeCount++] = archetype;
        for (int i = 0; i < _filterCount; i++)
        {
            _filters[i].Update(archetype);
        }
        worldArchetypesRWLock.ExitWriteLock();
        return archetype;
    }
}

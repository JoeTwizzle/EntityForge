using System;
using System.Collections.Concurrent;
using System.Buffers;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
using Archie.Helpers;
using System.Diagnostics.CodeAnalysis;
using CommunityToolkit.HighPerformance;

namespace Archie
{
    public sealed class World : IDisposable
    {
        /// <summary>
        /// Stores in which archetype an entity is
        /// </summary>
        internal ComponentIndexRecord[] EntityIndex;
        /// <summary>
        /// Stores all archetypes by their creation id
        /// </summary>
        internal Archetype[] AllArchetypes;
        /// <summary>
        /// Stores a filter based on the hash of a ComponentMask
        /// </summary>
        readonly Dictionary<ComponentMask, EntityFilter> FilterMap;
        /// <summary>
        /// Stores all archetypes by their ArchetypeId
        /// </summary>
        readonly Dictionary<int, uint> ArchetypeIndexMap;
        /// <summary>
        /// Stores the id a component has
        /// </summary>
        readonly Dictionary<Type, uint> ComponentMap;
        /// <summary>
        /// Used to find the archetypes containing a component and its index
        /// </summary>
        readonly Dictionary<Type, Dictionary<uint, TypeIndexRecord>> ComponentIndex;
        /// <summary>
        /// Used to find the archetypes containing a component
        /// </summary>
        readonly Dictionary<Type, List<uint>> CoarseComponentIndex;
        /// <summary>
        /// Contains now deleted entities whoose ids may be reused
        /// </summary>
        readonly List<EntityId> RecycledEntities;
        /// <summary>
        /// The id of this world
        /// </summary>
        public byte WorldId { get; private init; }
        /// <summary>
        /// Number of different archtypes in this world
        /// </summary>
        public uint ArchtypeCount => archetypeCount;

        int entityCounter;
        uint componentCounter;
        uint archetypeCount;

        static byte worldCounter;
        static readonly List<byte> recycledWorlds = new();

        public World()
        {
            WorldId = GetNextWorldId();
            AllArchetypes = new Archetype[256];
            EntityIndex = new ComponentIndexRecord[256];
            FilterMap = new(16);
            ComponentMap = new(16);
            ComponentIndex = new(16);
            CoarseComponentIndex = new(16);
            ArchetypeIndexMap = new(16);
            RecycledEntities = new(16);
        }

        #region Helpers

        static byte GetNextWorldId()
        {
            if (recycledWorlds.Count > 0)
            {
                byte id = recycledWorlds[recycledWorlds.Count - 1];
                recycledWorlds.RemoveAt(recycledWorlds.Count - 1);
                return id;
            }
            return worldCounter++;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public PackedEntity Pack(EntityId entity)
        {
            ValidateAliveDebug(entity);
            return new PackedEntity(entity.Id, EntityIndex[entity.Id].EntityVersion, WorldId);
        }

        public bool TryGetComponentID(Type type, out uint id)
        {
            return ComponentMap.TryGetValue(type, out id);
        }

        public uint GetComponentID(Type type)
        {
            if (TryGetComponentID(type, out var id))
            {
                return id;
            }
            id = componentCounter++;
            ComponentMap.Add(type, id);
            return id;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryUnpack(in PackedEntity entity, out EntityId entityId)
        {
            if (IsAlive(entity.Entity))
            {
                EntityId id = entity.Entity;
                if (entity.Version == EntityIndex[id.Id].EntityVersion)
                {
                    entityId = id;
                    return true;
                }
            }
            entityId = 0;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsAlive(EntityId entity)
        {
            return entity.Id < entityCounter && EntityIndex[entity.Id].EntityVersion > 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Archetype GetArchetype(EntityId entity)
        {
            return GetComponentIndexRecord(entity).Archetype;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref ComponentIndexRecord GetComponentIndexRecord(EntityId entity)
        {
            return ref EntityIndex[entity.Id];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Dictionary<uint, TypeIndexRecord> GetContainingArchetypesWithIndex(Type componentType)
        {
            return ComponentIndex[componentType];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public List<uint> GetContainingArchetypes(Type componentType)
        {
            return CoarseComponentIndex[componentType];
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetContainingArchetypes(Type componentType, [NotNullWhen(true)] out Dictionary<uint, TypeIndexRecord>? result)
        {
            return ComponentIndex.TryGetValue(componentType, out result);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T[] GetComponentPool<T>(Archetype archetype) where T : struct, IComponent<T>
        {
            return (T[])archetype.PropertyPool[GetTypeIndexRecord<T>(archetype).ComponentTypeIndex];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref TypeIndexRecord GetTypeIndexRecord<T>(Archetype archetype) where T : struct, IComponent<T>
        {
            var archetypes = GetContainingArchetypesWithIndex(typeof(T));
            return ref archetypes.Get(archetype.Index);
        }

        struct TypeComparer : IComparer<Type>
        {
            public int Compare(Type? x, Type? y)
            {
                bool xNull = x == null;
                bool yNull = y == null;
                if (xNull && yNull) return 0;
                if (xNull) return -1;
                if (yNull) return 1;
                return x!.FullName!.CompareTo(y!.FullName!);
            }
        }

        public static bool Contains(Archetype archetype, Type type)
        {
            return GetIndex(archetype, type).Id >= 0;
        }

        public static EntityId GetIndex(Archetype archetype, Type type)
        {
            return archetype.ComponentTypes.AsSpan().BinarySearch(type, new TypeComparer());
        }

        public static void SortTypes(Span<Type> componentTypes)
        {
            componentTypes.Sort((x, y) => x.FullName!.CompareTo(y.FullName));
        }

        public static int GetComponentHash(Span<Type> componentTypes)
        {
            unchecked
            {
                int hash = 0;
                for (int i = 0; i < componentTypes.Length; i++)
                {
                    hash ^= componentTypes[i].GetHashCode();
                }
                return hash;
            }
        }

        public static int GetComponentMaskHash(ComponentMask mask)
        {
            unchecked
            {
                int hash = 0;
                for (int i = 0; i < mask.Included.Length; i++)
                {
                    hash ^= mask.Included[i].GetHashCode();
                }
                for (int i = 0; i < mask.Excluded.Length; i++)
                {
                    hash ^= mask.Excluded[i].GetHashCode();
                }
                return hash;
            }
        }

        #endregion

        #region Debug Checks

        [Conditional("DEBUG")]
        private void ValidateAddDebug(Archetype archetype, Type type)
        {
            if (Contains(archetype, type))
            {
                ThrowHelper.ThrowDuplicateComponentException($"Tried adding duplicate Component of type {type}");
            }
        }

        [Conditional("DEBUG")]
        private void ValidateRemoveDebug(Archetype archetype, Type type)
        {
            if (!Contains(archetype, type))
            {
                ThrowHelper.ThrowMissingComponentException($"Tried removing missing Component of type {type}");
            }
        }

        [Conditional("DEBUG")]
        private void ValidateAliveDebug(EntityId entity)
        {
            if (!IsAlive(entity))
            {
                ThrowHelper.ThrowArgumentException($"Tried accessing destroyed entity: {entity}");
            }
        }

        [Conditional("DEBUG")]
        private void ValidateDestroyedDebug(EntityId entity)
        {
            if (IsAlive(entity))
            {
                ThrowHelper.ThrowArgumentException($"Tried accessing alive entity: {entity}");
            }
        }

        #endregion

        #region Entity Operations

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ReserveEntities(in ArchetypeDefinition definition, uint count)
        {
            var archetype = GetOrCreateArchetype(definition);
            archetype.GrowIfNeeded(count);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityId CreateEntityImmediate()
        {
            return CreateEntityImmediate(Archetype.CreateDefinition(Array.Empty<Type>()));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityId CreateEntityImmediate(in ArchetypeDefinition definition)
        {

            var archetype = GetOrCreateArchetype(definition);
            if (RecycledEntities.Count > 0)
            {
                EntityId entity = RecycledEntities[RecycledEntities.Count - 1];
                RecycledEntities.RemoveAt(RecycledEntities.Count - 1);
                archetype.GrowIfNeeded(1);
                ref var compIndex = ref EntityIndex[entity.Id];
                compIndex.Archetype = archetype;
                archetype.Entities[archetype.entityCount] = entity;
                compIndex.ComponentIndex = archetype.entityCount++;
                compIndex.EntityVersion = (short)-compIndex.EntityVersion;
                return entity;
            }
            else
            {
                var entity = entityCounter++;
                archetype.GrowIfNeeded(1);
                EntityIndex = EntityIndex.GrowIfNeeded(entityCounter, 1);
                EntityIndex[entity] = new ComponentIndexRecord(archetype, archetype.entityCount, 1);
                archetype.Entities[archetype.entityCount] = entity;
                archetype.entityCount++;
                return entity;
            }
        }

        internal uint MoveEntityImmediate(Archetype src, Archetype dest, EntityId entity)
        {
            Debug.Assert(src != dest);

            ref ComponentIndexRecord compIndexRecord = ref GetComponentIndexRecord(entity);
            uint oldIndex = compIndexRecord.ComponentIndex;
            //Add to new Archetype
            dest.GrowIfNeeded(1);
            dest.Entities[dest.entityCount] = entity;
            uint newIndex = dest.entityCount++;
            //Copy data to new Arrays
            for (int i = 0; i < dest.ComponentTypes.Length; i++)
            {
                if (ComponentIndex[dest.ComponentTypes[i]].TryGetValue(src.Index, out var typeIndexRecord))
                {
                    Array.Copy(src.PropertyPool[typeIndexRecord.ComponentTypeIndex], oldIndex, dest.PropertyPool[i], newIndex, 1);
                }
            }
            //Remove from old Archetype
            //Compact old Arrays
            for (int i = 0; i < src.PropertyPool.Length; i++)
            {
                var pool = src.PropertyPool[i];
                Array.Copy(pool, oldIndex + 1, pool, oldIndex, src.entityCount - (oldIndex + 1));
            }
            src.entityCount--;
            compIndexRecord.ComponentIndex = newIndex;
            compIndexRecord.Archetype = dest;

            return newIndex;
        }

        public void DestroyEntityImmediate(EntityId entity)
        {
            ValidateAliveDebug(entity);
            ref ComponentIndexRecord compIndexRecord = ref GetComponentIndexRecord(entity);
            var src = compIndexRecord.Archetype;
            uint compIndex = compIndexRecord.ComponentIndex;
            //Compact old Arrays
            for (int i = 0; i < src.PropertyPool.Length; i++)
            {
                var pool = src.PropertyPool[i];
                Array.Copy(pool, compIndex + 1, pool, compIndex, src.entityCount - (compIndex + 1));
            }
            ref var entityIndex = ref EntityIndex[entity.Id];
            entityIndex.EntityVersion = (short)-(entityIndex.EntityVersion + 1);
            src.entityCount--;
            RecycledEntities.Add(entity);
        }
        #endregion

        #region Component Operations

        public void AddComponentImmediate<T>(EntityId entity) where T : struct, IComponent<T>
        {


            ValidateAliveDebug(entity);
            var arch = GetArchetype(entity);
            ValidateAddDebug(arch, typeof(T));
            var newArch = GetOrCreateArchetypeVariantAdd(arch, typeof(T));

            var i = GetTypeIndexRecord<T>(newArch).ComponentTypeIndex;
            //Move entity to new archetype
            //Will want to delay this in future... maybe
            var index = MoveEntityImmediate(arch, newArch, entity);
            T.Init(ref ((T[])newArch.PropertyPool[i])[index]);
        }

        public void RemoveComponentImmediate<T>(EntityId entity) where T : struct, IComponent<T>
        {
            ValidateAliveDebug(entity);
            var arch = GetArchetype(entity);
            ValidateRemoveDebug(arch, typeof(T));
            var i = GetTypeIndexRecord<T>(arch).ComponentTypeIndex;
            var index = GetComponentIndexRecord(entity).ComponentIndex;
            T.Del(ref ((T[])arch.PropertyPool[i])[index]);
            var newArch = GetOrCreateArchetypeVariantRemove(arch, typeof(T));
            //Move entity to new archetype
            //Will want to delay this in future
            MoveEntityImmediate(arch, newArch, entity);
        }

        public bool HasComponent<T>(EntityId entity) where T : struct, IComponent<T>
        {
            ValidateAliveDebug(entity);
            return HasComponent(entity, typeof(T));
        }

        public bool HasComponent(EntityId entity, Type component)
        {
            ValidateAliveDebug(entity);
            ref ComponentIndexRecord record = ref GetComponentIndexRecord(entity);
            Archetype archetype = record.Archetype;
            var archetypes = ComponentIndex[component];
            return archetypes.ContainsKey(archetype.Index);
        }

        public ref T GetComponent<T>(EntityId entity) where T : struct, IComponent<T>
        {
            ValidateAliveDebug(entity);
            // First check if archetype has component
            ref ComponentIndexRecord record = ref GetComponentIndexRecord(entity);
            if (Unsafe.IsNullRef(ref record))
            {
                ThrowHelper.ThrowNullRefrenceException($"Entity Id:{entity} does not have a component of type {typeof(T).Name} attached");
            }
            //Get the pool of components
            var pool = GetComponentPool<T>(record.Archetype).AsSpan();
            return ref pool[(int)record.ComponentIndex];
        }

        #endregion

        #region Archetype Operations

        public Archetype GetOrCreateArchetype(in ArchetypeDefinition definition)
        {
            return GetArchetype(definition) ?? CreateArchetype(definition);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Archetype? GetArchetype(in ArchetypeDefinition definition)
        {
            return GetArchetype(definition.HashCode);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Archetype? GetArchetype(int hash)
        {
            if (ArchetypeIndexMap.TryGetValue(hash, out var index))
            {
                return AllArchetypes[index];
            }
            return null;
        }

        private Archetype GetOrCreateArchetypeVariantRemove(Archetype source, Type type)
        {
            //Archetype already stored in graph
            if (source.TryGetSiblingRemove(type, out var a))
            {
                return a;
            }
            //Graph failed we need to find Archetype by hash
            int length = source.ComponentTypes.Length - 1;
            Type[] pool = ArrayPool<Type>.Shared.Rent(length);
            int index = 0;
            for (int i = 0; i < source.ComponentTypes.Length; i++)
            {
                var compPool = source.ComponentTypes[i];
                if (compPool != type)
                {
                    pool[index++] = compPool;
                }
            }
            var span = pool.AsSpan(0, length);
            var definition = Archetype.CreateDefinition(span.ToArray());
            var arch = GetArchetype(definition.HashCode);
            //We found it!
            if (arch != null)
            {
                ArrayPool<Type>.Shared.Return(pool);
                return arch;
            }
            //Archetype does not yet exist, create it!
            var archetype = CreateArchetype(definition);
            ArrayPool<Type>.Shared.Return(pool);
            return archetype;
        }

        private Archetype GetOrCreateArchetypeVariantAdd(Archetype source, Type type)
        {
            //Archetype already stored in graph
            if (source.TryGetSiblingAdd(type, out var a))
            {
                return a;
            }
            int length = source.ComponentTypes.Length + 1;
            Type[] pool = ArrayPool<Type>.Shared.Rent(length);
            for (int i = 0; i < source.ComponentTypes.Length; i++)
            {
                pool[i] = source.ComponentTypes[i];
            }
            pool[length - 1] = type;
            var span = pool.AsSpan(0, length);
            var definition = Archetype.CreateDefinition(span.ToArray());
            var arch = GetArchetype(definition.HashCode);
            //We found it!
            if (arch != null)
            {
                ArrayPool<Type>.Shared.Return(pool);
                return arch;
            }
            //Archetype does not yet exist, create it!
            var archetype = CreateArchetype(definition);
            ArrayPool<Type>.Shared.Return(pool);
            return archetype;
        }

        internal Archetype CreateArchetype(in ArchetypeDefinition definition)
        {
            //Store type Definitions
            var mask = new BitMask();
            for (int i = 0; i < definition.Types.Length; i++)
            {
                mask.SetBit((int)GetComponentID(definition.Types[i]));
            }
            // Create
            var types = definition.Types;
            for (int i = 0; i < types.Length; i++)
            {
                mask.SetBit((int)ComponentMap[types[i]]);
            }
            var archetype = new Archetype(types, mask, definition.HashCode, archetypeCount);
            // Store in index
            for (uint i = 0; i < types.Length; i++)
            {
                var type = types[i];
                if (!ComponentIndex.TryGetValue(type, out var dict))
                {
                    dict = new();
                    ComponentIndex.Add(type, dict);
                }
                dict.Add(archetype.Index, new TypeIndexRecord(i));
                if (!CoarseComponentIndex.TryGetValue(type, out var list))
                {
                    list = new();
                    CoarseComponentIndex.Add(type, list);
                }
                list.Add(archetype.Index);
            }
            // Store in all archetypes
            ArchetypeIndexMap.Add(definition.HashCode, archetypeCount);
            AllArchetypes.GrowIfNeeded((int)archetypeCount, 1);
            AllArchetypes[archetypeCount++] = archetype;
            return archetype;
        }
        #endregion

        #region Filters

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityFilter Filter(ComponentMask mask)
        {
            if (!FilterMap.TryGetValue(mask, out var filter))
            {
                filter = new EntityFilter(this, mask);
                FilterMap.Add(mask, filter);
            }
            return filter;
        }

        public void Query<T, T1>(ComponentMask mask, ref T forEach) where T : struct, IComponentQuery<T1> where T1 : struct, IComponent<T1>
        {
            if (!FilterMap.TryGetValue(mask, out var filter))
            {
                filter = new EntityFilter(this, mask);
                FilterMap.Add(mask, filter);
            }
            for (int i = 0; i < archetypeCount; i++)
            {
                var arch = AllArchetypes[i];
                if (filter.Matches(arch.BitMask))
                {
                    var pool = (T1[])arch.PropertyPool[ComponentIndex[typeof(T1)][arch.Index].ComponentTypeIndex];
                    int count = (int)arch.entityCount;
                    var items = new Span<T1>(pool, 0, count);
                    for (int j = 0; j < count; j++)
                    {
                        forEach.Process(ref items[j]);
                    }
                }
            }
        }


        #endregion


        public void Dispose()
        {
            entityCounter = 0;
            recycledWorlds.Add(WorldId);
        }
    }
}

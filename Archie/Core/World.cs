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

namespace Archie
{
    public sealed class World : IDisposable
    {
        /// <summary>
        /// Stores which entities an archetype contains
        /// </summary>
        internal EntityRecord[] Entities;
        /// <summary>
        /// Stores in which archetype an entity is
        /// </summary>
        internal ComponentIndexRecord[] EntityIndex;
        /// <summary>
        /// Stores all archetypes by their creation id
        /// </summary>
        internal Archetype[] AllArchetypes;
        /// <summary>
        /// Stores all archetypes by their ArchetypeId
        /// </summary>
        readonly Dictionary<int, uint> ArchetypeIndexMap;
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
        uint archetypeCount;

        static byte worldCounter;
        static readonly List<byte> recycledWorlds = new();

        public World()
        {
            WorldId = GetNextWorldId();
            AllArchetypes = new Archetype[256];
            EntityIndex = new ComponentIndexRecord[256];
            Entities = new EntityRecord[256];
            ComponentIndex = new();
            CoarseComponentIndex = new();
            ArchetypeIndexMap = new();
            RecycledEntities = new();
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
            return (T[])archetype.ComponentPools[GetTypeIndexRecord<T>(archetype).ComponentTypeIndex];
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
            return archetype.Types.AsSpan().BinarySearch(type, new TypeComparer());
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
                compIndex.ComponentIndex = archetype.entityCount++;
                compIndex.EntityVersion = (short)-compIndex.EntityVersion;
                Entities[archetype.Index].Entities.Add(entity);
                return entity;
            }
            else
            {
                var entity = entityCounter++;
                archetype.GrowIfNeeded(1);
                EntityIndex = EntityIndex.GrowIfNeeded(entityCounter, 1);
                EntityIndex[entity] = new ComponentIndexRecord(archetype, archetype.entityCount, 1);
                Entities[archetype.Index].Entities.Add(entity);
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
            uint newIndex = dest.entityCount++;
            //Copy data to new Arrays
            for (int i = 0; i < dest.Types.Length; i++)
            {
                if (ComponentIndex[dest.Types[i]].TryGetValue(src.Index, out var typeIndexRecord))
                {
                    Array.Copy(src.ComponentPools[typeIndexRecord.ComponentTypeIndex], oldIndex, dest.ComponentPools[i], newIndex, 1);
                }
            }
            //Compact old Arrays
            for (int i = 0; i < src.ComponentPools.Length; i++)
            {
                var pool = src.ComponentPools[i];
                Array.Copy(pool, oldIndex + 1, pool, oldIndex, src.entityCount - (oldIndex + 1));
            }
            //Remove from old Archetype
            src.entityCount--;
            Entities[src.Index].Entities.Remove(entity);
            compIndexRecord.ComponentIndex = newIndex;
            compIndexRecord.Archetype = dest;
            Entities[dest.Index].Entities.Add(entity);
            return newIndex;
        }

        public void DestroyEntityImmediate(EntityId entity)
        {
            ValidateAliveDebug(entity);
            ref ComponentIndexRecord compIndexRecord = ref GetComponentIndexRecord(entity);
            var src = compIndexRecord.Archetype;
            uint compIndex = compIndexRecord.ComponentIndex;
            //Compact old Arrays
            for (int i = 0; i < src.ComponentPools.Length; i++)
            {
                var pool = src.ComponentPools[i];
                Array.Copy(pool, compIndex + 1, pool, compIndex, src.entityCount - (compIndex + 1));
            }
            ref var entityIndex = ref EntityIndex[entity.Id];
            entityIndex.EntityVersion = (short)-(entityIndex.EntityVersion + 1);
            src.entityCount--;
            Entities[src.Index].Entities.Remove(entity);
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
            T.Init(ref ((T[])newArch.ComponentPools[i])[index]);
        }

        public void RemoveComponentImmediate<T>(EntityId entity) where T : struct, IComponent<T>
        {
            ValidateAliveDebug(entity);
            var arch = GetArchetype(entity);
            ValidateRemoveDebug(arch, typeof(T));
            var i = GetTypeIndexRecord<T>(arch).ComponentTypeIndex;
            var index = GetComponentIndexRecord(entity).ComponentIndex;
            T.Del(ref ((T[])arch.ComponentPools[i])[index]);
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
            int length = source.Types.Length - 1;
            Type[] pool = ArrayPool<Type>.Shared.Rent(length);
            int index = 0;
            for (int i = 0; i < source.Types.Length; i++)
            {
                var compPool = source.Types[i];
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
            int length = source.Types.Length + 1;
            Type[] pool = ArrayPool<Type>.Shared.Rent(length);
            for (int i = 0; i < source.Types.Length; i++)
            {
                pool[i] = source.Types[i];
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
            // Create
            var types = definition.Types;
            var archetype = new Archetype(types, definition.HashCode, archetypeCount);
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
            Entities.GrowIfNeeded((int)archetypeCount, 1);
            Entities[archetypeCount] = new();
            AllArchetypes[archetypeCount++] = archetype;
            return archetype;
        }
        #endregion

        #region Filters

        public ComponentMask FilterInc<T>() where T : struct, IComponent<T>
        {
            return new ComponentMask(this).Inc<T>();
        }

        #endregion


        public void Dispose()
        {
            entityCounter = 0;
            recycledWorlds.Add(WorldId);
        }
    }
}

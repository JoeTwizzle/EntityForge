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
using Collections.Pooled;
using Archie.Collections;

namespace Archie
{
    public sealed class World : IDisposable
    {
        /// <summary>
        /// Stores in which architype an entity is
        /// </summary>
        ComponentIndexRecord[] EntityIndex;
        /// <summary>
        /// Archetype storing all entities with no components
        /// </summary>
        readonly FastPooledDictionary<ArchitypeId, Archetype> AllArchetypes;
        /// <summary>
        /// Used to find the archetypes containing a component
        /// </summary>
        readonly FastPooledDictionary<Type, FastPooledDictionary<ArchitypeId, TypeIndexRecord>> ComponentIndex;
        /// <summary>
        /// Contains now deleted entities whoose ids may be reused
        /// </summary>
        readonly PooledList<EntityId> RecycledEntities;
        /// <summary>
        /// The id of this world
        /// </summary>
        public byte WorldId { get; private init; }

        uint entityCounter;

        static byte worldCounter;
        static readonly List<byte> recycledWorlds = new();

        public World()
        {
            WorldId = GetNextWorldId();
            EntityIndex = new ComponentIndexRecord[256];
            ComponentIndex = new(ClearMode.Never);
            AllArchetypes = new(ClearMode.Never);
            RecycledEntities = new(ClearMode.Never);
        }

        #region Helpers

        bool ContainsEntity(in EntityId entity)
        {
            return entity.Entity < entityCounter && EntityIndex[entity.Entity].EntityVersion == entity.Version;
        }

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
        public bool IsAlive(in EntityId entity)
        {
            return ContainsEntity(entity);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Archetype GetArchetype(in EntityId entity)
        {
            return GetComponentIndexRecord(entity).Archetype;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref ComponentIndexRecord GetComponentIndexRecord(in EntityId entity)
        {
            return ref EntityIndex[entity.Entity];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public FastPooledDictionary<ArchitypeId, TypeIndexRecord> GetContainingArchetypes(Type componentType)
        {
            return ComponentIndex[componentType];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T[] GetComponentPool<T>(Archetype archetype) where T : struct, IComponent<T>
        {
            return (T[])archetype.ComponentPools[GetTypeIndexRecord<T>(archetype).ComponentTypeIndex];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref TypeIndexRecord GetTypeIndexRecord<T>(Archetype archetype) where T : struct, IComponent<T>
        {
            var archetypes = GetContainingArchetypes(typeof(T));
            return ref archetypes.Get(archetype.Id);
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
            return GetIndex(archetype, type) >= 0;
        }

        public static int GetIndex(Archetype archetype, Type type)
        {
            return archetype.Types.AsSpan().BinarySearch(type, new TypeComparer());
        }

        public static void SortTypes(Span<Type> componentTypes)
        {
            componentTypes.Sort((x, y) => x.FullName!.CompareTo(y.FullName));
        }

        public static int GetComponentHash(Span<Type> componentTypes)
        {
            int hash = 0;
            for (int i = 0; i < componentTypes.Length; i++)
            {
                hash ^= componentTypes[i].GetHashCode();
            }
            return hash;
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
        private void ValidateAliveDebug(in EntityId entity)
        {
            if (!IsAlive(entity))
            {
                ThrowHelper.ThrowArgumentException($"Tried accessing destroyed entity: {entity}");
            }
        }

        [Conditional("DEBUG")]
        private void ValidateDestroyedDebug(in EntityId entity)
        {
            if (IsAlive(entity))
            {
                ThrowHelper.ThrowArgumentException($"Tried accessing alive entity: {entity}");
            }
        }

        #endregion

        #region Component Operations
        public void AddComponentImmediate<T>(in EntityId entity) where T : struct, IComponent<T>
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

        public void RemoveComponentImmediate<T>(in EntityId entity) where T : struct, IComponent<T>
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

        public bool HasComponent<T>(in EntityId entity) where T : struct, IComponent<T>
        {
            ValidateAliveDebug(entity);
            return HasComponent(entity, typeof(T));
        }

        public bool HasComponent(EntityId entity, Type component)
        {
            ValidateAliveDebug(entity);
            ref ComponentIndexRecord record = ref GetComponentIndexRecord(entity);
            Archetype archetype = record.Archetype;
            FastPooledDictionary<ArchitypeId, TypeIndexRecord> archetypes = ComponentIndex[component];
            return archetypes.ContainsKey(archetype.Id);
        }

        public ref T GetComponent<T>(EntityId entity) where T : struct, IComponent<T>
        {
            ValidateAliveDebug(entity);
            // First check if archetype has component
            ref ComponentIndexRecord record = ref GetComponentIndexRecord(entity);
            if (Unsafe.IsNullRef(ref record))
            {
                ThrowHelper.ThrowNullRefrenceException($"Entity Id:{entity.Id} does not have a component of type {typeof(T).Name} attached");
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
            return AllArchetypes.GetValueOrDefault(definition.HashCode);
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
            var arch = AllArchetypes.GetValueOrDefault(definition.HashCode);
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
            var arch = AllArchetypes.GetValueOrDefault(definition.HashCode);
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
            var archetype = new Archetype(types, definition.HashCode);
            // Store in index
            for (uint i = 0; i < types.Length; i++)
            {
                var type = types[i];
                if (!ComponentIndex.TryGetValue(type, out var dict))
                {
                    dict = new();
                    ComponentIndex.Add(type, dict);
                }
                dict.Add(archetype.Id, new TypeIndexRecord(i));
            }
            // Store in all archetypes
            AllArchetypes.Add(definition.HashCode, archetype);
            return archetype;
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
            var entity = GetNextEntity();
            var ts = Array.Empty<Type>();
            var archetype = GetOrCreateArchetype(Archetype.CreateDefinition(ts));
            archetype.GrowIfNeeded(1);
            EntityIndex = EntityIndex.GrowIfNeeded(entityCounter, 1);
            EntityIndex[archetype.entityCount] = new ComponentIndexRecord(archetype, archetype.entityCount++, entity.Version);
            return entity;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityId CreateEntityImmediate(in ArchetypeDefinition definition)
        {
            var entity = GetNextEntity();
            var archetype = GetOrCreateArchetype(definition);
            archetype.GrowIfNeeded(1);
            EntityIndex = EntityIndex.GrowIfNeeded(entityCounter, 1);
            EntityIndex[archetype.entityCount] = new ComponentIndexRecord(archetype, archetype.entityCount++, entity.Version);
            return entity;
        }

        internal EntityId GetNextEntity()
        {
            if (RecycledEntities.Count > 0)
            {
                EntityId entity = EntityId.Recycle(RecycledEntities[RecycledEntities.Count - 1]);
                RecycledEntities.RemoveAt(RecycledEntities.Count - 1);
                return entity;
            }
            return new EntityId(entityCounter++, 0, WorldId);
        }

        internal uint MoveEntityImmediate(Archetype src, Archetype dest, in EntityId entity)
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
                if (ComponentIndex[dest.Types[i]].TryGetValue(src.Id, out var typeIndexRecord))
                {
                    Array.Copy(src.ComponentPools[typeIndexRecord.ComponentTypeIndex], oldIndex, dest.ComponentPools[i], newIndex, 1);
                }
            }
            //Compact old Arrays
            for (int i = 0; i < src.ComponentPools.Length; i++)
            {
                var pool = src.ComponentPools[i];
                Array.Copy(pool, oldIndex + 1, pool, oldIndex, src.entityCount - (oldIndex - 1));
            }
            //Remove from old Archetype
            src.entityCount--;
            compIndexRecord.ComponentIndex = newIndex;
            compIndexRecord.Archetype = dest;
            return newIndex;
        }

        public void DestroyEntityImmediate(in EntityId entity)
        {
            ValidateAliveDebug(entity);
            ref ComponentIndexRecord compIndexRecord = ref GetComponentIndexRecord(entity);
            var src = compIndexRecord.Archetype;
            uint index = compIndexRecord.ComponentIndex;
            for (int i = 0; i < src.ComponentPools.Length; i++)
            {
                var pool = src.ComponentPools[i];
                Array.Copy(pool, index + 1, pool, index, src.entityCount - (index - 1));
            }
            src.entityCount--;
            RecycledEntities.Add(entity);
        }

        public void Dispose()
        {
            RecycledEntities.Dispose();
            AllArchetypes.Dispose();
            ComponentIndex.Dispose();
            entityCounter = 0;
            recycledWorlds.Add(WorldId);
        }

        #endregion
    }
}

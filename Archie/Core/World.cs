using Archie.Helpers;
using CommunityToolkit.HighPerformance;
using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Archie
{

    public sealed partial class World : IDisposable
    {
        /// <summary>
        /// Stores in which archetype an entity is
        /// </summary>
        private ComponentIndexRecord[] EntityIndex;
        /// <summary>
        /// Stores all archetypes by their creation id
        /// </summary>
        internal Archetype[] AllArchetypes;
        /// <summary>
        /// Stores all filters by their creation id
        /// </summary>
        internal EntityFilter[] AllFilters;
        /// <summary>
        /// Stores a filter based on the hash of a ComponentMask
        /// </summary>
        readonly Dictionary<ComponentMask, int> FilterMap;
        /// <summary>
        /// Stores all archetypes by their ArchetypeId
        /// </summary>
        readonly Dictionary<int, int> ArchetypeIndexMap;
        /// <summary>
        /// Stores the id a component has
        /// </summary>
        readonly Dictionary<Type, int> ComponentMap;
        /// <summary>
        /// Used to find the archetypes containing a componentId and its index
        /// </summary>
        readonly Dictionary<int, Dictionary<int, TypeIndexRecord>> ComponentIndex;
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
        public int ArchtypeCount => archetypeCount;
        /// <summary>
        /// Span of all archetypes currently present in the world
        /// </summary>
        public Span<Archetype> Archetypes => new Span<Archetype>(AllArchetypes, 0, archetypeCount);
        int filterCount;
        int entityCounter;
        int componentCounter;
        int archetypeCount;

        static byte worldCounter;
        static readonly List<byte> recycledWorlds = new();

        public World()
        {
            WorldId = GetNextWorldId();
            AllArchetypes = new Archetype[256];
            EntityIndex = new ComponentIndexRecord[256];
            AllFilters = new EntityFilter[16];
            FilterMap = new(16);
            ComponentMap = new(16);
            ComponentIndex = new(16);
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

        public bool TryGetComponentID(Type type, out int id)
        {
            return ComponentMap.TryGetValue(type, out id);
        }

        public int GetOrCreateComponentID(Type type)
        {
            ref var id = ref CollectionsMarshal.GetValueRefOrAddDefault(ComponentMap, type, out var exists);
            if (!exists)
            {
                id = componentCounter++;
            }
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
        public Dictionary<int, TypeIndexRecord> GetContainingArchetypesWithIndex(int componentType)
        {
            return ComponentIndex[componentType];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetContainingArchetypes(int componentType, [NotNullWhen(true)] out Dictionary<int, TypeIndexRecord>? result)
        {
            return ComponentIndex.TryGetValue(componentType, out result);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T[] GetComponentPool<T>(Archetype archetype, int componentId) where T : struct, IComponent<T>
        {
            return (T[])archetype.PropertyPool[GetTypeIndexRecord(archetype, componentId).ComponentTypeIndex];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref TypeIndexRecord GetTypeIndexRecord(Archetype archetype, int componentId)
        {
            var archetypes = GetContainingArchetypesWithIndex(componentId);
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
                return string.Compare(x!.FullName, y!.FullName!, StringComparison.Ordinal);
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
            componentTypes.Sort((x, y) => string.Compare(x!.FullName, y!.FullName!, StringComparison.Ordinal));
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
        private static void ValidateAddDebug(Archetype archetype, Type type)
        {
            if (Contains(archetype, type))
            {
                ThrowHelper.ThrowDuplicateComponentException($"Tried adding duplicate Component of type {type}");
            }
        }

        [Conditional("DEBUG")]
        private static void ValidateRemoveDebug(Archetype archetype, Type type)
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
        public void ReserveEntities(in ArchetypeDefinition definition, int count)
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
                archetype.EntitiesBuffer[archetype.InternalEntityCount] = entity;
                compIndex.ArchetypeColumn = archetype.InternalEntityCount++;
                compIndex.EntityVersion = (short)-compIndex.EntityVersion;
                return entity;
            }
            else
            {
                var entity = entityCounter++;
                archetype.GrowIfNeeded(1);
                EntityIndex = EntityIndex.GrowIfNeeded(entityCounter, 1);
                EntityIndex[entity] = new ComponentIndexRecord(archetype, archetype.InternalEntityCount, 1);
                archetype.EntitiesBuffer[archetype.InternalEntityCount] = entity;
                archetype.InternalEntityCount++;
                return entity;
            }
        }

        private int MoveEntityImmediate(Archetype src, Archetype dest, EntityId entity)
        {
            Debug.Assert(src != dest);

            ref ComponentIndexRecord compIndexRecord = ref GetComponentIndexRecord(entity);
            int oldIndex = compIndexRecord.ArchetypeColumn;

            //Add to new Archetype
            dest.GrowIfNeeded(1);
            dest.EntitiesBuffer[dest.InternalEntityCount] = entity;
            int newIndex = dest.InternalEntityCount++;
            //Copy data to new Arrays
            src.CopyComponents(oldIndex, dest, newIndex);
            //Fill hole in old Arrays
            src.FillHole(oldIndex);

            //Update index of entity filling the hole
            ref ComponentIndexRecord rec = ref GetComponentIndexRecord(src.Entities[src.InternalEntityCount - 1]);
            rec.ArchetypeColumn = oldIndex;
            //Update index of moved entity
            compIndexRecord.ArchetypeColumn = newIndex;
            compIndexRecord.Archetype = dest;
            //Finish removing entity from source
            src.InternalEntityCount--;
            return newIndex;
        }

        public void DestroyEntityImmediate(EntityId entity)
        {
            ValidateAliveDebug(entity);

            ref ComponentIndexRecord compIndexRecord = ref GetComponentIndexRecord(entity);
            var src = compIndexRecord.Archetype;
            int oldIndex = compIndexRecord.ArchetypeColumn;
            //Get index of entity to be removed
            ref var entityIndex = ref EntityIndex[entity.Id];
            //Set its version to its negative increment (Mark entity as destroyed)
            entityIndex.EntityVersion = (short)-(entityIndex.EntityVersion + 1);
            //Fill hole in component array
            src.FillHole(oldIndex);
            //Update index of entity filling the hole
            ref ComponentIndexRecord rec = ref GetComponentIndexRecord(src.Entities[src.InternalEntityCount - 1]);
            rec.ArchetypeColumn = oldIndex;
            src.InternalEntityCount--;
            RecycledEntities.Add(entity);
        }

        #endregion

        #region Component Operations

        public void SetComponentImmediate<T>(EntityId entity, T value) where T : struct, IComponent<T>
        {
            ValidateAliveDebug(entity);
            ref var compIndexRecord = ref GetComponentIndexRecord(entity);
            int compId = GetOrCreateComponentID(typeof(T));
            var arch = compIndexRecord.Archetype;
            ComponentIndex.TryGetValue(compId, out var archetypes);
            if (archetypes != null && archetypes.TryGetValue(compIndexRecord.Archetype.Index, out var typeIndex))
            {
                ref T data = ref ((T[])arch.PropertyPool[typeIndex.ComponentTypeIndex])[compIndexRecord.ArchetypeColumn];
                data = value;
            }
            else
            {
                ValidateAddDebug(arch, typeof(T));
                var newArch = GetOrCreateArchetypeVariantAdd(arch, compId, typeof(T));
                var i = GetTypeIndexRecord(newArch, compId).ComponentTypeIndex;
                //Move entity to new archetype
                //Will want to delay this in future... maybe
                var index = MoveEntityImmediate(arch, newArch, entity);
                ref T data = ref ((T[])newArch.PropertyPool[i])[index];
                data = value;
            }
        }

        public void SetComponentImmediate<T>(EntityId entity) where T : struct, IComponent<T>
        {
            SetComponentImmediate(entity, new T());
        }

        public void UnsetComponentImmediate<T>(EntityId entity) where T : struct, IComponent<T>
        {
            ValidateAliveDebug(entity);
            var arch = GetArchetype(entity);
            int compId = GetOrCreateComponentID(typeof(T));
            var archetypes = ComponentIndex[compId];
            if (!archetypes.ContainsKey(arch.Index))
            {
                ValidateRemoveDebug(arch, typeof(T));
                var i = GetTypeIndexRecord(arch, compId).ComponentTypeIndex;
                var index = GetComponentIndexRecord(entity).ArchetypeColumn;
                var newArch = GetOrCreateArchetypeVariantRemove(arch, compId, typeof(T));
                //Move entity to new archetype
                //Will want to delay this in future.. maybe
                MoveEntityImmediate(arch, newArch, entity);
            }
        }

        public void AddComponentImmediate<T>(EntityId entity) where T : struct, IComponent<T>
        {
            AddComponentImmediate(entity, new T());
        }

        public void AddComponentImmediate<T>(EntityId entity, T value) where T : struct, IComponent<T>
        {
            ValidateAliveDebug(entity);
            var arch = GetArchetype(entity);
            ValidateAddDebug(arch, typeof(T));
            int compId = GetOrCreateComponentID(typeof(T));
            var newArch = GetOrCreateArchetypeVariantAdd(arch, compId, typeof(T));
            var i = GetTypeIndexRecord(newArch, compId).ComponentTypeIndex;
            //Move entity to new archetype
            //Will want to delay this in future... maybe
            var index = MoveEntityImmediate(arch, newArch, entity);
            ref T data = ref ((T[])newArch.PropertyPool[i])[index];
            data = value;
        }

        public void RemoveComponentImmediate<T>(EntityId entity) where T : struct, IComponent<T>
        {
            ValidateAliveDebug(entity);
            var arch = GetArchetype(entity);
            ValidateRemoveDebug(arch, typeof(T));
            int compId = GetOrCreateComponentID(typeof(T));
            var i = GetTypeIndexRecord(arch, compId).ComponentTypeIndex;
            var index = GetComponentIndexRecord(entity).ArchetypeColumn;
            ref var data = ref ((T[])arch.PropertyPool[i])[index];
            var newArch = GetOrCreateArchetypeVariantRemove(arch, compId, typeof(T));
            //Move entity to new archetype
            //Will want to delay this in future... maybe
            MoveEntityImmediate(arch, newArch, entity);
        }

        public bool HasComponent<T>(EntityId entity) where T : struct, IComponent<T>
        {
            return HasComponent(entity, typeof(T));
        }

        public bool HasComponent(EntityId entity, Type component)
        {
            ValidateAliveDebug(entity);
            ref ComponentIndexRecord record = ref GetComponentIndexRecord(entity);
            Archetype archetype = record.Archetype;
            int compId = GetOrCreateComponentID(component);
            var archetypes = ComponentIndex[compId];
            return archetypes.ContainsKey(archetype.Index);
        }

        public ref T GetComponent<T>(EntityId entity) where T : struct, IComponent<T>
        {
            ValidateAliveDebug(entity);
            // First check if archetype has component
            ref var record = ref EntityIndex[entity.Id];
            return ref record.Archetype.GetComponent<T>(record.ArchetypeColumn);
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

        private Archetype GetOrCreateArchetypeVariantRemove(Archetype source, int compId, Type type)
        {
            //Archetype already stored in graph
            if (source.TryGetSiblingRemove(compId, out var a))
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
            source.SetSiblingRemove(compId, archetype);
            return archetype;
        }

        private Archetype GetOrCreateArchetypeVariantAdd(Archetype source, int compId, Type type)
        {
            //Archetype already stored in graph
            if (source.TryGetSiblingAdd(compId, out var a))
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
            source.SetSiblingAdd(compId, archetype);
            return archetype;
        }

        private Archetype CreateArchetype(in ArchetypeDefinition definition)
        {
            //Store type Definitions
            var mask = new BitMask();
            int[] compIds = new int[definition.Types.Length];
            for (int i = 0; i < definition.Types.Length; i++)
            {
                int id = GetOrCreateComponentID(definition.Types[i]);
                compIds[i] = id;
                mask.SetBit(id);
            }
            // Create
            var types = definition.Types;
            for (int i = 0; i < types.Length; i++)
            {
                mask.SetBit(ComponentMap[types[i]]);
            }
            var archetype = new Archetype(compIds, types, mask, definition.HashCode, archetypeCount);
            // Store in index
            for (int i = 0; i < types.Length; i++)
            {
                var type = compIds[i];
                ref var dict = ref CollectionsMarshal.GetValueRefOrAddDefault(ComponentIndex, type, out var exists);
                if (!exists)
                {
                    dict = new();
                }
                dict!.Add(archetype.Index, new TypeIndexRecord(i));
            }
            // Store in all archetypes
            ArchetypeIndexMap.Add(definition.HashCode, archetypeCount);
            AllArchetypes.GrowIfNeeded((int)archetypeCount, 1);
            AllArchetypes[archetypeCount++] = archetype;
            for (int i = 0; i < filterCount; i++)
            {
                AllFilters[i].Update(archetype);
            }
            return archetype;
        }
        #endregion

        #region Filters

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityFilter GetFilter(ComponentMask mask)
        {
            ref var filterId = ref CollectionsMarshal.GetValueRefOrAddDefault(FilterMap, mask, out bool exists);
            if (exists)
            {
                return AllFilters[filterId];
            }
            var filter = new EntityFilter(this, mask);
            AllFilters = AllFilters.GrowIfNeeded(filterCount, 1);
            AllFilters[filterCount] = filter;
            filterId = filterCount++;
            return filter;
        }

        //See Core/Queries.cs for Queries
        #endregion

        public void Dispose()
        {
            entityCounter = 0;
            recycledWorlds.Add(WorldId);
        }
    }
}

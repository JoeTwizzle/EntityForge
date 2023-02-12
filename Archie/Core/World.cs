using Archie.Helpers;
using Archie.Relations;
using CommunityToolkit.HighPerformance;
using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Archie
{
    file struct TypeComparer : IComparer<Type>
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

    public sealed partial class World : IDisposable
    {
        private static byte worldCounter;
        private static readonly List<byte> recycledWorlds = new();
        private static readonly ArchetypeDefinition emptyArchetypeDefinition = Archetype.CreateDefinition(Array.Empty<Type>());
        /// <summary>
        /// Stores in which archetype an entity is
        /// </summary>
        private ComponentIndexRecord[] EntityIndex;
        /// <summary>
        /// Stores a filter based on the hash of a ComponentMask
        /// </summary>
        private readonly Dictionary<ComponentMask, int> FilterMap;
        /// <summary>
        /// Maps an Archetype's Hashcode to its index
        /// </summary>
        private readonly Dictionary<int, int> ArchetypeIndexMap;
        /// <summary>
        /// Stores the id a component has
        /// </summary>
        private readonly Dictionary<(Type, int), int> TypeMap;
        /// <summary>
        /// Used to find the archetypes containing a componentId and its index
        /// </summary>
        private readonly Dictionary<int, Dictionary<int, TypeIndexRecord>> TypeIndexMap;
        /// <summary>
        /// Contains now deleted entities whoose ids may be reused
        /// </summary>
        private readonly List<EntityId> RecycledEntities;
        /// <summary>
        /// Stores all archetypes by their creation id
        /// </summary>
        internal Archetype[] AllArchetypes;
        /// <summary>
        /// Stores all filters by their creation id
        /// </summary>
        internal EntityFilter[] AllFilters;
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
        int archetypeCount;
        int entityCounter;
        int componentCounter;

        public World()
        {
            WorldId = GetNextWorldId();
            AllArchetypes = new Archetype[256];
            EntityIndex = new ComponentIndexRecord[256];
            AllFilters = new EntityFilter[16];
            FilterMap = new(16);
            TypeMap = new(16);
            TypeIndexMap = new(16);
            ArchetypeIndexMap = new(16);
            RecycledEntities = new(16);
        }

        #region Helpers

        private static byte GetNextWorldId()
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
        public bool TryGetComponentID(Type type, int variant, out int id)
        {
            return TypeMap.TryGetValue((type, variant), out id);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetOrCreateComponentID(Type type)
        {
            return GetOrCreateComponentID(type, -1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetOrCreateComponentID(Type type, int variant)
        {
            ref var id = ref CollectionsMarshal.GetValueRefOrAddDefault(TypeMap, (type, variant), out var exists);
            if (!exists)
            {
                id = componentCounter++;
                TypeIndexMap.Add(id, new Dictionary<int, TypeIndexRecord>());
            }
            return id;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long CreateRelationId(int compTypeId, int target)
        {
            return compTypeId | (target << 32);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryUnpack(in PackedEntity entity, out EntityId entityId)
        {
            entityId = new EntityId(entity.Entity);
            if (IsAlive(entityId))
            {
                if (entity.Version == EntityIndex[entityId.Id].EntityVersion)
                {
                    return true;
                }
            }
            entityId = new EntityId(0);
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
            return TypeIndexMap[componentType];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetContainingArchetypes(int componentType, [NotNullWhen(true)] out Dictionary<int, TypeIndexRecord>? result)
        {
            return TypeIndexMap.TryGetValue(componentType, out result);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T[] GetComponentPool<T>(Archetype archetype, int componentId) where T : struct, IComponent<T>
        {
            return (T[])archetype.PropertyPool[GetTypeIndexRecord(archetype, componentId).ComponentTypeIndex];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref TypeIndexRecord GetTypeIndexRecord(Archetype archetype, int componentId)
        {
            return ref GetContainingArchetypesWithIndex(componentId).Get(archetype.Index);
        }

        public static bool Contains(Archetype archetype, Type type)
        {
            return GetIndex(archetype, type) >= 0;
        }

        public static int GetIndex(Archetype archetype, Type type)
        {
            return archetype.ComponentTypes.BinarySearch(type, new TypeComparer());
        }

        public static void SortTypes(Span<Type> componentTypes)
        {
            componentTypes.Sort((x, y) => string.Compare(x!.FullName, y!.FullName!, StringComparison.Ordinal));
        }

        public static Type[] RemoveDuplicates(Type[] types)
        {
            int head = 0;
            Span<int> indices = types.Length < 32 ? stackalloc int[32] : new int[types.Length];
            Guid prevType = Guid.Empty;
            for (int i = 0; i < types.Length; i++)
            {
                //This only works if the array is sorted
                if (prevType == types[i].GUID)
                {
                    continue;
                }
                indices[head++] = i;
            }
            //Contained no duplicates
            if (head == types.Length)
            {
                return types;
            }
            var deDup = new Type[head];
            for (int i = 0; i < deDup.Length; i++)
            {
                deDup[i] = types[indices[--head]];
            }
            return deDup;
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
            return CreateEntityImmediate(emptyArchetypeDefinition);
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
                var entityId = entityCounter++;
                archetype.GrowIfNeeded(1);
                EntityIndex = EntityIndex.GrowIfNeeded(entityCounter, 1);
                EntityIndex[entityId] = new ComponentIndexRecord(archetype, archetype.InternalEntityCount, 1);
                var ent = new EntityId(entityId);
                archetype.EntitiesBuffer[archetype.InternalEntityCount] = ent;
                archetype.InternalEntityCount++;
                return ent;
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
            var archetypes = TypeIndexMap[compId];
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
            var archetypes = TypeIndexMap[compId];
            if (archetypes.ContainsKey(arch.Index))
            {
                var i = GetTypeIndexRecord(arch, compId).ComponentTypeIndex;
                var index = GetComponentIndexRecord(entity).ArchetypeColumn;
                ref var data = ref ((T[])arch.PropertyPool[i])[index];
                var newArch = GetOrCreateArchetypeVariantRemove(arch, compId, typeof(T));
                //Move entity to new archetype
                //Will want to delay this in future... maybe
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
            //Move entity to new archetype
            //Will want to delay this in future... maybe
            var i = GetTypeIndexRecord(newArch, compId).ComponentTypeIndex;
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

        private void AddComponentImmediate<T>(EntityId entity, T value, int variant) where T : struct, IComponent<T>
        {
            var arch = GetArchetype(entity);
            int compId = GetOrCreateComponentID(typeof(T), variant);
            var newArch = GetOrCreateArchetypeVariantAdd(arch, compId, typeof(T));
            //Move entity to new archetype
            //Will want to delay this in future... maybe
            var i = GetTypeIndexRecord(newArch, compId).ComponentTypeIndex;
            var index = MoveEntityImmediate(arch, newArch, entity);
            ref T data = ref ((T[])newArch.PropertyPool[i])[index];
            data = value;
        }

        private void RemoveComponentImmediate<T>(EntityId entity, int variant) where T : struct, IComponent<T>
        {
            var arch = GetArchetype(entity);
            int compId = GetOrCreateComponentID(typeof(T), variant);
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
            var archetypes = TypeIndexMap[compId];
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

        #region Relation Operations

        public void AddRelationTarget<T>(EntityId entity, EntityId target) where T : struct, IRelation<T>, IComponent<T>
        {
            if (T.RelationKind == RelationKind.SingleMulti)
            {
                if (HasComponent<OneToManyRelation<T>>(entity))
                {
                    ref OneToManyRelation<T> rel = ref GetComponent<OneToManyRelation<T>>(entity);
                    rel.Add(target);
                    return;
                }
            }
            AddRelationTarget(entity, target, new T());
        }

        public void AddRelationTarget<T>(EntityId entity, EntityId target, T value) where T : struct, IRelation<T>, IComponent<T>
        {
            switch (T.RelationKind)
            {
                case RelationKind.SingleSingleDiscriminated:
                    AddComponentImmediate(entity, new DiscriminatingOneToOneRelation<T>(value), target.Id);
                    break;
                case RelationKind.SingleSingle:
                    AddComponentImmediate(entity, new OneToOneRelation<T>(value, target));
                    break;
                case RelationKind.SingleMulti:
                    AddComponentImmediate(entity, new OneToManyRelation<T>(value, new EntityId[1] { target }));
                    break;
                case RelationKind.MultiMulti:
                    if (HasComponent<ManyToManyRelation<T>>(entity))
                    {
                        ref ManyToManyRelation<T> rel = ref GetComponent<ManyToManyRelation<T>>(entity);
                        rel.Add(target, value);
                    }
                    else
                    {
                        AddComponentImmediate(entity, new ManyToManyRelation<T>(new T[1] { value }, new EntityId[1] { target }));
                    }
                    break;
                default:
                    AddComponentImmediate(entity, new OneToOneRelation<T>(value, target));
                    break;
            };
        }

        public void RemoveRelationTarget<T>(EntityId entity, EntityId target) where T : struct, IRelation<T>, IComponent<T>
        {
            if (T.RelationKind == RelationKind.SingleSingleDiscriminated)
            {
                RemoveComponentImmediate<DiscriminatingOneToOneRelation<T>>(entity, target.Id);
                return;
            }
            switch (T.RelationKind)
            {
                case RelationKind.SingleSingle:
                    RemoveComponentImmediate<OneToOneRelation<T>>(entity);
                    break;
                case RelationKind.SingleMulti:
                    ref OneToManyRelation<T> rel = ref GetComponent<OneToManyRelation<T>>(entity);
                    rel.Remove(target);
                    if (rel.Length <= 0)
                    {
                        RemoveComponentImmediate<OneToManyRelation<T>>(entity);
                    }
                    break;
                case RelationKind.MultiMulti:
                    ref ManyToManyRelation<T> rel2 = ref GetComponent<ManyToManyRelation<T>>(entity);
                    rel2.Remove(target);
                    if (rel2.Length <= 0)
                    {
                        RemoveComponentImmediate<ManyToManyRelation<T>>(entity);
                    }
                    break;
                default:
                    RemoveComponentImmediate<OneToOneRelation<T>>(entity);
                    break;
            };
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

        private Archetype GetOrCreateArchetypeVariantAdd(Archetype source, int compId, Type type)
        {
            Archetype? archetype;
            //Archetype already stored in graph
            if (source.TryGetSiblingAdd(compId, out archetype))
            {
                return archetype;
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
            archetype = GetArchetype(definition.HashCode);
            //We found it!
            if (archetype != null)
            {
                ArrayPool<Type>.Shared.Return(pool);
                return archetype;
            }
            //Archetype does not yet exist, create it!
            archetype = CreateArchetype(definition);
            ArrayPool<Type>.Shared.Return(pool);
            source.SetSiblingAdd(compId, archetype);
            return archetype;
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
            var otherTypes = new Type[1] { typeof(EntityId) };
            var archetype = new Archetype(compIds, definition.Types, definition.DiscriminatingRelations, otherTypes, mask, definition.HashCode, archetypeCount);
            // Store in index
            for (int i = 0; i < compIds.Length; i++)
            {
                TypeIndexMap[compIds[i]].Add(archetype.Index, new TypeIndexRecord(i));
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

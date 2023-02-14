using Archie.Helpers;
using Archie.Relations;
using CommunityToolkit.HighPerformance;
using System.Buffers;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Archie
{
    file struct TypeComparer : IComparer<ComponentId>
    {
        public int Compare(ComponentId x, ComponentId y)
        {
            int val = x.TypeId > y.TypeId ? 1 : (x.TypeId < y.TypeId ? -1 : 0);
            if (val == 0) return x.Variant > y.Variant ? 1 : (x.Variant < y.Variant ? -1 : 0);
            return val;
        }
    }

    public sealed partial class World : IDisposable
    {
        public const int DefaultComponents = 16;
        public const int DefaultEntities = 256;
        public const int DefaultVariant = 0;
        private static byte worldCounter;
        private static int componentCounter;
        private static readonly List<byte> recycledWorlds = new();
        private static readonly object lockObj = new object();
        private static readonly World[] worlds = new World[byte.MaxValue + 1];
        private static readonly ArchetypeDefinition emptyArchetypeDefinition = new ArchetypeDefinition(GetComponentHash(Array.Empty<ComponentId>()), Array.Empty<ComponentId>());
        /// <summary>
        /// Stores the id a component has
        /// </summary>
        private static readonly Dictionary<Type, int> TypeMap = new();

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
        /// Used to find the archetypes containing a componentId and its index
        /// </summary>
        private readonly Dictionary<ComponentId, Dictionary<int, TypeIndexRecord>> TypeIndexMap;
        /// <summary>
        /// Used to find the archetypes containing a componentId and its index
        /// </summary>
        private readonly Dictionary<int, Dictionary<int, Dictionary<int, TypeIndexRecord>>> TypeIndexMapByTypeId;
        /// <summary>
        /// Used to find the archetypes containing a componentId and its index
        /// </summary>
        private readonly Dictionary<int, Dictionary<int, Dictionary<int, TypeIndexRecord>>> TypeIndexMapByVariant;
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
        /// Span of all entities currently present in the world
        /// </summary>
        public Span<ComponentIndexRecord> EntityIndices => new Span<ComponentIndexRecord>(EntityIndex, 0, entityCounter);
        /// <summary>
        /// Span of all archetypes currently present in the world
        /// </summary>
        public Span<Archetype> Archetypes => new Span<Archetype>(AllArchetypes, 0, archetypeCount);
        /// <summary>
        /// Span of all filters currently present in the world
        /// </summary>
        public Span<EntityFilter> Filters => new Span<EntityFilter>(AllFilters, 0, filterCount);
        int filterCount;
        int archetypeCount;
        int entityCounter;

        public World()
        {
            WorldId = GetNextWorldId();
            AllArchetypes = new Archetype[DefaultComponents];
            AllFilters = new EntityFilter[DefaultComponents];
            FilterMap = new(DefaultComponents);
            ArchetypeIndexMap = new(DefaultComponents);
            TypeIndexMap = new(DefaultComponents);
            TypeIndexMapByTypeId = new(DefaultComponents);
            TypeIndexMapByVariant = new(DefaultComponents);
            EntityIndex = new ComponentIndexRecord[DefaultEntities];
            RecycledEntities = new(DefaultEntities);
            worlds[WorldId] = this;
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
        public static int GetOrCreateTypeId<T>() where T : struct, IComponent<T>
        {
            if (!T.Registered)
            {
                lock (lockObj)
                {
                    ref var id = ref CollectionsMarshal.GetValueRefOrAddDefault(TypeMap, typeof(T), out var exists);
                    if (!exists)
                    {
                        id = componentCounter++;
                        T.Registered = true;
                    }
                    T.Id = id;
                }
            }
            return T.Id;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetOrCreateTypeId(Type type)
        {
            lock (lockObj)
            {
                ref var id = ref CollectionsMarshal.GetValueRefOrAddDefault(TypeMap, type, out var exists);
                if (!exists)
                {
                    id = componentCounter++;
                }
                return id;
            }
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
        public Dictionary<int, TypeIndexRecord> GetContainingArchetypesWithIndex(ComponentId componentType)
        {
            return TypeIndexMap[componentType];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetContainingArchetypes(ComponentId componentType, [NotNullWhen(true)] out Dictionary<int, TypeIndexRecord>? result)
        {
            return TypeIndexMap.TryGetValue(componentType, out result);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T[] GetComponentPool<T>(Archetype archetype, ComponentId componentId) where T : struct, IComponent<T>
        {
            return (T[])archetype.PropertyPool[GetTypeIndexRecord(archetype, componentId).ComponentTypeIndex];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref TypeIndexRecord GetTypeIndexRecord(Archetype archetype, ComponentId componentId)
        {
            return ref GetContainingArchetypesWithIndex(componentId).Get(archetype.Index);
        }

        public static bool Contains(Archetype archetype, ComponentId type)
        {
            return GetIndex(archetype, type) >= 0;
        }

        public static int GetIndex(Archetype archetype, ComponentId type)
        {
            return archetype.ComponentTypes.BinarySearch(type, new TypeComparer());
        }

        public static void SortTypes(Span<ComponentId> componentTypes)
        {
            componentTypes.Sort((x, y) =>
            {
                int val = x.TypeId > y.TypeId ? 1 : (x.TypeId < y.TypeId ? -1 : 0);
                if (val == 0) return x.Variant > y.Variant ? 1 : (x.Variant < y.Variant ? -1 : 0);
                return val;
            });
        }

        public static ComponentId[] RemoveDuplicates(ComponentId[] types)
        {
            int head = 0;
            Span<int> indices = types.Length < 32 ? stackalloc int[32] : new int[types.Length];
            if (types.Length > 0)
            {
                ComponentId prevType = types[0];
                indices[head++] = 0;
                prevType = types[0];
                for (int i = 1; i < types.Length; i++)
                {
                    //This only works if the array is sorted
                    if (prevType == types[i])
                    {
                        continue;
                    }
                    indices[head++] = i;
                    prevType = types[i];
                }
            }
            //Contained no duplicates
            if (head == types.Length)
            {
                return types;
            }
            var deDup = new ComponentId[head];
            for (int i = 0; i < deDup.Length; i++)
            {
                deDup[i] = types[indices[--head]];
            }
            return deDup;
        }

        public static int GetComponentHash(Span<ComponentId> componentTypes)
        {
            unchecked
            {
                int hash = 17;
                for (int i = 0; i < componentTypes.Length; i++)
                {
                    hash = hash * 486187739 + componentTypes[i].GetHashCode();
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
        private static void ValidateAddDebug(Archetype archetype, ComponentId type)
        {
            if (Contains(archetype, type))
            {
                ThrowHelper.ThrowDuplicateComponentException($"Tried adding duplicate Component of type {type}");
            }
        }

        [Conditional("DEBUG")]
        private static void ValidateRemoveDebug(Archetype archetype, ComponentId type)
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
            Debug.Assert(src.Index != dest.Index);

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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetComponentImmediate<T>(EntityId entity, T value, int variant = World.DefaultVariant) where T : struct, IComponent<T>
        {
            ValidateAliveDebug(entity);
            ref var compIndexRecord = ref GetComponentIndexRecord(entity);
            var compId = new ComponentId(GetOrCreateTypeId<T>(), variant, typeof(T));
            var arch = compIndexRecord.Archetype;
            var archetypes = TypeIndexMap[compId];
            if (archetypes != null && archetypes.TryGetValue(compIndexRecord.Archetype.Index, out var typeIndex))
            {
                ref T data = ref ((T[])arch.PropertyPool[typeIndex.ComponentTypeIndex])[compIndexRecord.ArchetypeColumn];
                data = value;
            }
            else
            {
                ValidateAddDebug(arch, compId);
                var newArch = GetOrCreateArchetypeVariantAdd(arch, compId);
                var i = GetTypeIndexRecord(newArch, compId).ComponentTypeIndex;
                //Move entity to new archetype
                //Will want to delay this in future... maybe
                var index = MoveEntityImmediate(arch, newArch, entity);
                ref T data = ref ((T[])newArch.PropertyPool[i])[index];
                data = value;
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetComponentImmediate<T>(EntityId entity) where T : struct, IComponent<T>
        {
            SetComponentImmediate(entity, new T());
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UnsetComponentImmediate<T>(EntityId entity, int variant = World.DefaultVariant) where T : struct, IComponent<T>
        {
            ValidateAliveDebug(entity);
            var arch = GetArchetype(entity);
            var compId = new ComponentId(GetOrCreateTypeId<T>(), variant, typeof(T));
            var archetypes = TypeIndexMap[compId];
            if (archetypes.ContainsKey(arch.Index))
            {
                var i = GetTypeIndexRecord(arch, compId).ComponentTypeIndex;
                var index = GetComponentIndexRecord(entity).ArchetypeColumn;
                ref var data = ref ((T[])arch.PropertyPool[i])[index];
                var newArch = GetOrCreateArchetypeVariantRemove(arch, compId);
                //Move entity to new archetype
                //Will want to delay this in future... maybe
                MoveEntityImmediate(arch, newArch, entity);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddComponentImmediate<T>(EntityId entity, int variant = World.DefaultVariant) where T : struct, IComponent<T>
        {
            AddComponentImmediate(entity, new T());
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddComponentImmediate<T>(EntityId entity, T value, int variant = World.DefaultVariant) where T : struct, IComponent<T>
        {
            ValidateAliveDebug(entity);
            var arch = GetArchetype(entity);
            var compId = new ComponentId(GetOrCreateTypeId<T>(), variant, typeof(T));
            ValidateAddDebug(arch, compId);
            var newArch = GetOrCreateArchetypeVariantAdd(arch, compId);
            //Move entity to new archetype
            //Will want to delay this in future... maybe
            var i = GetTypeIndexRecord(newArch, compId).ComponentTypeIndex;
            var index = MoveEntityImmediate(arch, newArch, entity);
            ref T data = ref ((T[])newArch.PropertyPool[i])[index];
            data = value;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveComponentImmediate<T>(EntityId entity, int variant = World.DefaultVariant) where T : struct, IComponent<T>
        {
            ValidateAliveDebug(entity);
            var arch = GetArchetype(entity);
            var compId = new ComponentId(GetOrCreateTypeId<T>(), variant, typeof(T));
            ValidateRemoveDebug(arch, compId);
            var i = GetTypeIndexRecord(arch, compId).ComponentTypeIndex;
            var index = GetComponentIndexRecord(entity).ArchetypeColumn;
            var newArch = GetOrCreateArchetypeVariantRemove(arch, compId);
            //Move entity to new archetype
            //Will want to delay this in future... maybe
            MoveEntityImmediate(arch, newArch, entity);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasComponent<T>(EntityId entity, int variant = World.DefaultVariant) where T : struct, IComponent<T>
        {
            int compId = GetOrCreateTypeId<T>();
            ref ComponentIndexRecord record = ref GetComponentIndexRecord(entity);
            Archetype archetype = record.Archetype;
            var archetypes = TypeIndexMap[new ComponentId(compId, variant, typeof(T))];
            return archetypes.ContainsKey(archetype.Index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasComponent(EntityId entity, ComponentId component)
        {
            ValidateAliveDebug(entity);
            ref ComponentIndexRecord record = ref GetComponentIndexRecord(entity);
            Archetype archetype = record.Archetype;

            var archetypes = TypeIndexMap[component];
            return archetypes.ContainsKey(archetype.Index);
        }

        public ref T GetComponent<T>(EntityId entity, int variant = World.DefaultVariant) where T : struct, IComponent<T>
        {
            ValidateAliveDebug(entity);
            // First check if archetype has component
            ref var record = ref EntityIndex[entity.Id];
            return ref record.Archetype.GetComponent<T>(record.ArchetypeColumn, GetOrCreateTypeId<T>());
        }

        public void RemoveAll<T>(int variant = World.DefaultVariant) where T : struct, IComponent<T>
        {
            var archetypes = GetContainingArchetypesWithIndex(new ComponentId(GetOrCreateTypeId<T>(), variant, typeof(T)));
            foreach (var item in archetypes)
            {
                var ents = AllArchetypes[item.Key].Entities;
                for (int i = ents.Length - 1; i >= 0; i--)
                {
                    RemoveComponentImmediate<T>(ents[i]);
                }
            }
        }

        public void ClearEmptyArchetypes()
        {
            var archs = Archetypes;
            for (int i = archs.Length - 1; i >= 0; i--)
            {
                if (archs[i].EntityCount <= 0)
                {
                    archs[i].Reset();
                }
            }
        }

        #endregion

        #region Relation Operations

        public bool HasRelation<T>(EntityId entity, EntityId target) where T : struct, IRelation<T>, IComponent<T>
        {
            switch (T.RelationKind)
            {
                case RelationKind.SingleSingleDiscriminated:
                    return HasComponent<DiscriminatingOneToOneRelation<T>>(entity, target.Id);
                case RelationKind.SingleSingle:
                    return HasComponent<OneToOneRelation<T>>(entity) && GetComponent<OneToOneRelation<T>>(entity).TargetEntity == target;
                case RelationKind.SingleMulti:
                    return HasComponent<OneToManyRelation<T>>(entity) && GetComponent<OneToManyRelation<T>>(entity).EntityIndexMap.ContainsKey(Pack(target));
                case RelationKind.MultiMulti:
                    return HasComponent<ManyToManyRelation<T>>(entity) && GetComponent<ManyToManyRelation<T>>(entity).EntityIndexMap.ContainsKey(Pack(target));
            };
            return false;
        }

        public void RemoveAllRelations<T>() where T : struct, IRelation<T>, IComponent<T>
        {
            switch (T.RelationKind)
            {
                case RelationKind.SingleSingle:
                    RemoveAll<OneToOneRelation<T>>();
                    break;
                case RelationKind.SingleMulti:
                    RemoveAll<OneToManyRelation<T>>();
                    break;
                case RelationKind.MultiMulti:
                    RemoveAll<ManyToManyRelation<T>>();
                    break;
            };
        }


        public void AddRelationTarget<T>(EntityId entity, EntityId target) where T : struct, IRelation<T>, IComponent<T>
        {
            if (T.RelationKind == RelationKind.SingleMulti)
            {
                if (HasComponent<OneToManyRelation<T>>(entity))
                {
                    ref OneToManyRelation<T> rel = ref GetComponent<OneToManyRelation<T>>(entity);
                    rel.Add(Pack(target));
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
                    AddComponentImmediate(entity, new DiscriminatingOneToOneRelation<T>(value, Pack(target)), target.Id);
                    break;
                case RelationKind.SingleSingle:
                    AddComponentImmediate(entity, new OneToOneRelation<T>(value, Pack(target)));
                    break;
                case RelationKind.SingleMulti:
                    if (HasComponent<ManyToManyRelation<T>>(entity))
                    {
                        ref OneToManyRelation<T> rel = ref GetComponent<OneToManyRelation<T>>(entity);
                        rel.Add(Pack(target));
                    }
                    else
                    {
                        AddComponentImmediate(entity, new OneToManyRelation<T>(value, new PackedEntity[1] { Pack(target) }));
                    }
                    break;
                case RelationKind.MultiMulti:
                    if (HasComponent<ManyToManyRelation<T>>(entity))
                    {
                        ref ManyToManyRelation<T> rel = ref GetComponent<ManyToManyRelation<T>>(entity);
                        rel.Add(Pack(target), value);
                    }
                    else
                    {
                        AddComponentImmediate(entity, new ManyToManyRelation<T>(new T[1] { value }, new PackedEntity[1] { Pack(target) }));
                    }
                    break;
            };
        }

        public void RemoveRelationTarget<T>(EntityId entity, EntityId target) where T : struct, IRelation<T>, IComponent<T>
        {
            switch (T.RelationKind)
            {
                case RelationKind.SingleSingleDiscriminated:
                    RemoveComponentImmediate<DiscriminatingOneToOneRelation<T>>(entity, target.Id);
                    break;
                case RelationKind.SingleSingle:
                    RemoveComponentImmediate<OneToOneRelation<T>>(entity);
                    break;
                case RelationKind.SingleMulti:
                    ref OneToManyRelation<T> rel = ref GetComponent<OneToManyRelation<T>>(entity);
                    rel.Remove(Pack(target));
                    if (rel.Length <= 0)
                    {
                        RemoveComponentImmediate<OneToManyRelation<T>>(entity);
                    }
                    break;
                case RelationKind.MultiMulti:
                    ref ManyToManyRelation<T> rel2 = ref GetComponent<ManyToManyRelation<T>>(entity);
                    rel2.Remove(Pack(target));
                    if (rel2.Length <= 0)
                    {
                        RemoveComponentImmediate<ManyToManyRelation<T>>(entity);
                    }
                    break;
            };
        }

        public ref T GetRelation<T>(EntityId entity, EntityId target) where T : struct, IRelation<T>, IComponent<T>
        {
            switch (T.RelationKind)
            {
                case RelationKind.SingleSingleDiscriminated:
                    return ref GetComponent<DiscriminatingOneToOneRelation<T>>(entity, target.Id).RelationData;
                case RelationKind.SingleSingle:
                    return ref GetComponent<OneToOneRelation<T>>(entity).RelationData;
                case RelationKind.SingleMulti:
                    return ref GetComponent<OneToManyRelation<T>>(entity).RelationData;
                case RelationKind.MultiMulti:
                    ref ManyToManyRelation<T> rel2 = ref GetComponent<ManyToManyRelation<T>>(entity);
                    return ref rel2.RelationData[rel2.EntityIndexMap[Pack(target)]];
            };
            return ref Unsafe.NullRef<T>();
        }

        public Span<T> GetRelationSpan<T>(EntityId entity) where T : struct, IRelation<T>, IComponent<T>
        {
            switch (T.RelationKind)
            {
                case RelationKind.SingleSingleDiscriminated:
                    ThrowHelper.ThrowArgumentException("The given relation is a discriminating relation, and thus needs a target.");
                    break;
                case RelationKind.SingleSingle:
                    return new Span<T>(ref GetComponent<OneToOneRelation<T>>(entity).RelationData);
                case RelationKind.SingleMulti:
                    return new Span<T>(ref GetComponent<OneToManyRelation<T>>(entity).RelationData);
                case RelationKind.MultiMulti:
                    ref ManyToManyRelation<T> rel2 = ref GetComponent<ManyToManyRelation<T>>(entity);
                    return rel2.RelationData.AsSpan();
            };
            return Span<T>.Empty;
        }

        public Span<PackedEntity> GetRelationTargets<T>(EntityId entity) where T : struct, IRelation<T>, IComponent<T>
        {
            switch (T.RelationKind)
            {
                case RelationKind.SingleSingleDiscriminated:
                    ThrowHelper.ThrowArgumentException("The given relation is a discriminating relation, and thus needs a target.");
                    break;
                case RelationKind.SingleSingle:
                    return new Span<PackedEntity>(ref GetComponent<OneToOneRelation<T>>(entity).TargetEntity);
                case RelationKind.SingleMulti:
                    return GetComponent<OneToManyRelation<T>>(entity).TargetEntities.AsSpan();
                case RelationKind.MultiMulti:
                    ref ManyToManyRelation<T> rel2 = ref GetComponent<ManyToManyRelation<T>>(entity);
                    return rel2.TargetEntities.AsSpan();
            };
            return Span<PackedEntity>.Empty;
        }

        #endregion

        #region Archetype Operations

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Archetype GetOrCreateArchetype(in ArchetypeDefinition definition)
        {
            return GetArchetype(definition) ?? CreateArchetype(definition);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Archetype? GetArchetype(in ArchetypeDefinition definition)
        {
            return GetArchetypeByHashCode(definition.HashCode);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Archetype? GetArchetypeByHashCode(int hash)
        {
            if (ArchetypeIndexMap.TryGetValue(hash, out var index))
            {
                return AllArchetypes[index];
            }
            return null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Archetype GetOrCreateArchetypeVariantAdd(Archetype source, ComponentId compId)
        {
            Archetype? archetype;
            //Archetype already stored in graph
            if (source.TryGetSiblingAdd(compId, out archetype))
            {
                return archetype;
            }
            int length = source.ComponentTypes.Length + 1;
            ComponentId[] pool = ArrayPool<ComponentId>.Shared.Rent(length);
            for (int i = 0; i < source.ComponentTypes.Length; i++)
            {
                pool[i] = source.ComponentTypes[i];
            }
            pool[length - 1] = compId;
            var span = pool.AsSpan(0, length);
            int hash = GetComponentHash(span);
            archetype = GetArchetypeByHashCode(hash);
            //We found it!
            if (archetype != null)
            {
                ArrayPool<ComponentId>.Shared.Return(pool);
                return archetype;
            }
            //Archetype does not yet exist, create it!
            var definition = new ArchetypeDefinition(hash, span.ToArray());
            archetype = CreateArchetype(definition);
            ArrayPool<ComponentId>.Shared.Return(pool);
            source.SetSiblingAdd(compId, archetype);
            return archetype;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Archetype GetOrCreateArchetypeVariantRemove(Archetype source, ComponentId compId)
        {
            //Archetype already stored in graph
            if (source.TryGetSiblingRemove(compId, out var a))
            {
                return a;
            }
            //Graph failed we need to find Archetype by hash
            int length = source.ComponentTypes.Length - 1;
            ComponentId[] pool = ArrayPool<ComponentId>.Shared.Rent(length);
            int index = 0;
            for (int i = 0; i < source.ComponentTypes.Length; i++)
            {
                var compPool = source.ComponentTypes[i];
                if (compPool != compId)
                {
                    pool[index++] = compPool;
                }
            }
            var span = pool.AsSpan(0, length);
            int hash = GetComponentHash(span);
            var arch = GetArchetypeByHashCode(hash);
            //We found it!
            if (arch != null)
            {
                ArrayPool<ComponentId>.Shared.Return(pool);
                return arch;
            }
            //Archetype does not yet exist, create it!
            var definition = new ArchetypeDefinition(hash, span.ToArray());
            var archetype = CreateArchetype(definition);
            ArrayPool<ComponentId>.Shared.Return(pool);
            source.SetSiblingRemove(compId, archetype);
            return archetype;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Archetype CreateArchetype(in ArchetypeDefinition definition)
        {
            //Store type Definitions
            var mask = new BitMask();
            var compIds = definition.ComponentIds;
            for (int i = 0; i < definition.ComponentIds.Length; i++)
            {
                int id = definition.ComponentIds[i].TypeId;
                mask.SetBit(id);
            }
            var archetype = new Archetype(definition.ComponentIds, mask, definition.HashCode, archetypeCount);
            // Store in index
            for (int i = 0; i < compIds.Length; i++)
            {
                ref var dict = ref CollectionsMarshal.GetValueRefOrAddDefault(TypeIndexMap, compIds[i], out var exists);
                if (!exists)
                {
                    dict = new Dictionary<int, TypeIndexRecord>();
                }
                dict!.Add(archetype.Index, new TypeIndexRecord(i));

                ref var dictTypeId = ref CollectionsMarshal.GetValueRefOrAddDefault(TypeIndexMapByTypeId, compIds[i].TypeId, out exists);
                if (!exists)
                {
                    dictTypeId = new();
                }
                ref var dictVariant = ref CollectionsMarshal.GetValueRefOrAddDefault(dictTypeId!, compIds[i].Variant, out exists);
                if (!exists)
                {
                    dictVariant = dict;
                }
                ref var dictTypeId2 = ref CollectionsMarshal.GetValueRefOrAddDefault(TypeIndexMapByTypeId, compIds[i].Variant, out exists);
                if (!exists)
                {
                    dictTypeId2 = new();
                }
                ref var dictVariant2 = ref CollectionsMarshal.GetValueRefOrAddDefault(dictTypeId2!, compIds[i].TypeId, out exists);
                if (!exists)
                {
                    dictVariant2 = dict;
                }
            }
            // Store in all archetypes
            ArchetypeIndexMap.Add(definition.HashCode, archetypeCount);
            AllArchetypes = AllArchetypes.GrowIfNeeded(archetypeCount, 1);
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

        public void Reset()
        {
            RecycledEntities.Clear();
            FilterMap.Clear();
            TypeIndexMap.Clear();
            TypeMap.Clear();
            ArchetypeIndexMap.Clear();
            Archetypes.Clear();
            Filters.Clear();
            EntityIndices.Clear();
            entityCounter = 0;
            componentCounter = 0;
            archetypeCount = 0;
            filterCount = 0;
        }
    }
}

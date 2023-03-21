using Archie.Helpers;
using Archie.Queries;
using Archie.Relations;
using CommunityToolkit.HighPerformance;
using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Archie
{
    unsafe struct ComponentFunc
    {
        public delegate*<ArrayOrPointer, int, void> Func;

        public ComponentFunc(delegate*<ArrayOrPointer, int, void> func)
        {
            Func = func;
        }
    }

    public sealed partial class World : IDisposable
    {
        public const int DefaultComponents = 16;
        public const int DefaultEntities = 256;
        public const int DefaultVariant = 0;
        private static int worldCounter;
        private static int componentCounter;
        private static readonly List<int> recycledWorlds = new();
        private static readonly object lockObj = new object();
        internal static World[] worlds = new World[1];
        internal unsafe static ComponentFunc[] OnInitFuncs = new ComponentFunc[DefaultComponents];
        internal unsafe static ComponentFunc[] OnDeleteFuncs = new ComponentFunc[DefaultComponents];
        private static readonly ArchetypeDefinition emptyArchetypeDefinition = new ArchetypeDefinition(GetComponentHash(Array.Empty<ComponentInfo>()), Array.Empty<ComponentInfo>());
        /// <summary>
        /// Stores the id value component has
        /// </summary>
        private static readonly Dictionary<Type, int> TypeMap = new();
        /// <summary>
        /// Stores the type an id has
        /// </summary>
        private static readonly Dictionary<int, Type> TypeMapReverse = new();
        /// <summary>
        /// Stores in which archetype an entity is
        /// </summary>
        private EntityIndexRecord[] EntityIndex;
        /// <summary>
        /// Stores value filter based on the hash of value ComponentMask
        /// </summary>
        private readonly Dictionary<ComponentMask, int> FilterMap;
        /// <summary>
        /// Maps an Archetype's Hashcode to its index
        /// </summary>
        private readonly Dictionary<int, int> ArchetypeIndexMap;
        /// <summary>
        /// Used to find the variantMap containing value componentId and its index
        /// </summary>
        private readonly Dictionary<ComponentId, Dictionary<int, TypeIndexRecord>> TypeIndexMap;
        /// <summary>
        /// Contains now deleted Entities whoose ids may be reused
        /// </summary>
        private EntityId[] RecycledEntities;
        /// <summary>
        /// Stores all variantMap by their creation id
        /// </summary>
        internal Archetype[] AllArchetypes;
        /// <summary>
        /// Stores all filters by their creation id
        /// </summary>
        internal EntityFilter[] AllFilters;
        /// <summary>
        /// The id of this world
        /// </summary>
        public int WorldId { get; private init; }
        /// <summary>
        /// Number of different archtypes in this world
        /// </summary>
        public int ArchtypeCount => archetypeCount;
        /// <summary>
        /// Span of all EntitiesPool currently present in the world
        /// </summary>
        public Span<EntityIndexRecord> EntityIndices => new Span<EntityIndexRecord>(EntityIndex, 0, entityCounter);
        /// <summary>
        /// Span of all variantMap currently present in the world
        /// </summary>
        public Span<Archetype> Archetypes => new Span<Archetype>(AllArchetypes, 0, archetypeCount);
        /// <summary>
        /// Span of all filters currently present in the world
        /// </summary>
        public Span<EntityFilter> Filters => new Span<EntityFilter>(AllFilters, 0, filterCount);
        int filterCount;
        int archetypeCount;
        int entityCounter;
        int recycledEntitiesCount;
        bool isLocked;

        public World()
        {
            AllArchetypes = new Archetype[DefaultComponents];
            AllFilters = new EntityFilter[DefaultComponents];
            FilterMap = new(DefaultComponents);
            ArchetypeIndexMap = new(DefaultComponents);
            TypeIndexMap = new(DefaultComponents);
            EntityIndex = new EntityIndexRecord[DefaultEntities];
            RecycledEntities = new EntityId[DefaultEntities];

            WorldId = GetNextWorldId();
            worlds = worlds.GrowIfNeeded(worldCounter, 1);
            worlds[WorldId] = this;
            isLocked = false;
        }

        #region Static Operations

        private static int GetNextWorldId()
        {
            if (recycledWorlds.Count > 0)
            {
                int lastIdx = recycledWorlds.Count - 1;
                int id = recycledWorlds[lastIdx];
                recycledWorlds.RemoveAt(lastIdx);
                return id;
            }
            return worldCounter++;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetOrCreateTypeId<T>() where T : struct, IComponent<T>
        {
            if (!T.Registered)
            {
                CreateTypeId<T>();
            }
            return T.Id;
        }

        static void CreateTypeId<T>() where T : struct, IComponent<T>
        {
            lock (lockObj)
            {
                ref var id = ref CollectionsMarshal.GetValueRefOrAddDefault(TypeMap, typeof(T), out var exists);
                if (!exists)
                {
                    id = componentCounter;
                    TypeMapReverse.Add(id, typeof(T));
                    T.Registered = true;

                    //Grow void* array OnInitFuncs
                    OnInitFuncs = OnInitFuncs.GrowIfNeeded(componentCounter, 1);
                    unsafe
                    {
                        OnInitFuncs[componentCounter] = new ComponentFunc((delegate*<ArrayOrPointer, int, void>)&IComponent<T>.InternalOnInit);
                    }
                    OnDeleteFuncs = OnDeleteFuncs.GrowIfNeeded(componentCounter, 1);
                    unsafe
                    {
                        OnDeleteFuncs[componentCounter++] = new ComponentFunc((delegate*<ArrayOrPointer, int, void>)&IComponent<T>.InternalOnDelete);
                    }
                    T.Id = id;
                }
            }
        }

        public static ComponentId GetOrCreateComponentId<T>(int variant) where T : struct, IComponent<T>
        {
            return new ComponentId(GetOrCreateTypeId<T>(), variant);
        }

        public static ComponentInfo GetOrCreateComponentInfo<T>(int variant) where T : struct, IComponent<T>
        {
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                return new ComponentInfo(GetOrCreateComponentId<T>(variant), typeof(T));
            }
            else
            {
                return new ComponentInfo(GetOrCreateComponentId<T>(variant), Unsafe.SizeOf<T>());
            }
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

        public static void SortTypes(Span<ComponentInfo> componentTypes)
        {
            componentTypes.Sort((x, y) =>
            {
                int val = x.ComponentId.TypeId > y.ComponentId.TypeId ? 1 : (x.ComponentId.TypeId < y.ComponentId.TypeId ? -1 : 0);
                if (val == 0) return x.ComponentId.Variant > y.ComponentId.Variant ? 1 : (x.ComponentId.Variant < y.ComponentId.Variant ? -1 : 0);
                return val;
            });
        }

        public static ComponentInfo[] RemoveDuplicates(ComponentInfo[] types)
        {
            int head = 0;
            Span<int> indices = types.Length < 32 ? stackalloc int[32] : new int[types.Length];
            if (types.Length > 0)
            {
                ComponentInfo prevType = types[0];
                indices[head++] = 0;
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
            var deDup = new ComponentInfo[head];
            for (int i = 0; i < deDup.Length; i++)
            {
                deDup[i] = types[indices[--head]];
            }
            return deDup;
        }

        public static int GetComponentHash(Span<ComponentInfo> componentTypes)
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

        #endregion

        #region Helpers

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public PackedEntity Pack(EntityId entity)
        {
            ValidateAliveDebug(entity);
            return new PackedEntity(entity.Id, EntityIndex[entity.Id].EntityVersion, WorldId);
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
            return EntityIndex[entity.Id].EntityVersion > 0 && entity.Id < entityCounter;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Archetype GetArchetype(EntityId entity)
        {
            return GetComponentIndexRecord(entity).Archetype;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref EntityIndexRecord GetComponentIndexRecord(EntityId entity)
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
        public ref TypeIndexRecord GetTypeIndexRecord(Archetype archetype, ComponentId componentId)
        {
            return ref GetContainingArchetypesWithIndex(componentId).Get(archetype.Index);
        }
        #endregion

        #region Debug Checks

        [Conditional("DEBUG")]
        private static void ValidateHasDebug(Archetype archetype, ComponentId type)
        {
            if (!Archetype.Contains(archetype, type))
            {
                ThrowHelper.ThrowDuplicateComponentException($"Tried adding duplicate Component of type {type}");
            }
        }

        [Conditional("DEBUG")]
        private static void ValidateAddDebug(Archetype archetype, ComponentId type)
        {
            if (Archetype.Contains(archetype, type))
            {
                ThrowHelper.ThrowDuplicateComponentException($"Tried adding duplicate Component of type {type}");
            }
        }

        [Conditional("DEBUG")]
        private static void ValidateRemoveDebug(Archetype archetype, ComponentId type)
        {
            if (!Archetype.Contains(archetype, type))
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
        public Entity CreateEntity()
        {
            return CreateEntity(emptyArchetypeDefinition);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Entity CreateEntity(in ArchetypeDefinition definition)
        {
            var archetype = GetOrCreateArchetype(definition);
            EntityId entity;
            int archetypeColumn;
            if (recycledEntitiesCount > 0)
            {
                entity = RecycledEntities[--recycledEntitiesCount];
                archetype.GrowIfNeeded(1);
                ref var compIndex = ref EntityIndex[entity.Id];
                compIndex.Archetype = archetype;
                compIndex.EntityVersion = (short)-compIndex.EntityVersion;
                archetype.EntityBuffer[archetype.ElementCount] = new Entity(entity.Id, WorldId);
                compIndex.ArchetypeColumn = archetype.ElementCount;
            }
            else
            {
                var entityId = entityCounter;
                archetype.GrowIfNeeded(1);
                EntityIndex = EntityIndex.GrowIfNeeded(entityCounter++, 1);
                EntityIndex[entityId] = new EntityIndexRecord(archetype, archetype.ElementCount, 1);
                entity = new EntityId(entityId);
                archetype.EntityBuffer[archetype.ElementCount] = new Entity(entity.Id, WorldId);
            }
            archetypeColumn = archetype.ElementCount++;
            unsafe
            {
                for (int i = 0; i < archetype.ComponentInfo.Length; i++)
                {
                    var comp = archetype.ComponentInfo[i];
                    OnInitFuncs[comp.ComponentId.TypeId].Func(archetype.PropertyPools[i], archetypeColumn);
                }
            }
            return new Entity(entity.Id, WorldId);
        }


        private int MoveEntityImmediate(Archetype src, Archetype dest, EntityId entity)
        {
            Debug.Assert(src.Index != dest.Index);

            ref EntityIndexRecord compIndexRecord = ref GetComponentIndexRecord(entity);
            int oldIndex = compIndexRecord.ArchetypeColumn;

            //Add to new Archetype
            dest.GrowIfNeeded(1);
            dest.EntityBuffer[dest.ElementCount] = new Entity(entity.Id, WorldId);
            int newIndex = dest.ElementCount++;
            //Copy Data to new Arrays
            src.CopyComponents(oldIndex, dest, newIndex);
            //Fill hole in old Arrays
            src.FillHole(oldIndex);

            //Update index of entity filling the hole
            ref EntityIndexRecord rec = ref GetComponentIndexRecord(src.Entities[src.ElementCount - 1]);
            rec.ArchetypeColumn = oldIndex;
            //Update index of moved entity
            compIndexRecord.ArchetypeColumn = newIndex;
            compIndexRecord.Archetype = dest;
            //Finish removing entity from source
            src.ElementCount--;
            return newIndex;
        }

        public void DestroyEntity(EntityId entity)
        {
            ValidateAliveDebug(entity);

            ref EntityIndexRecord compIndexRecord = ref GetComponentIndexRecord(entity);
            var src = compIndexRecord.Archetype;
            int oldIndex = compIndexRecord.ArchetypeColumn;
            unsafe
            {
                for (int i = 0; i < src.ComponentInfo.Length; i++)
                {
                    var comp = src.ComponentInfo[i].ComponentId;
                    OnDeleteFuncs[comp.TypeId].Func(src.PropertyPools[i], oldIndex);
                }
            }
            //Get index of entity to be removed
            ref var entityIndex = ref EntityIndex[entity.Id];
            //Set its version to its negative increment (Mark entity as destroyed)
            entityIndex.EntityVersion = (short)-(entityIndex.EntityVersion + 1);
            //Fill hole in component array
            src.FillHole(oldIndex);
            //Update index of entity filling the hole
            ref EntityIndexRecord rec = ref GetComponentIndexRecord(src.Entities[src.ElementCount - 1]);
            rec.ArchetypeColumn = oldIndex;
            src.ElementCount--;
            RecycledEntities = RecycledEntities.GrowIfNeeded(recycledEntitiesCount, 1);
            RecycledEntities[recycledEntitiesCount++] = entity;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Entity GetEntity(EntityId id)
        {
            return new Entity(id.Id, WorldId);
        }

        #endregion

        #region Component Operations

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool SetComponent<T>(EntityId entity, int variant = World.DefaultVariant) where T : struct, IComponent<T>
        {
            ref var compIndexRecord = ref GetComponentIndexRecord(entity);
            var arch = compIndexRecord.Archetype;
            var compInfo = World.GetOrCreateComponentInfo<T>(variant);

            if (!HasComponent<T>(entity, variant))
            {
                (int index, Archetype newArch) = AddComponentInternal(entity, compInfo, arch);
                unsafe
                {
                    OnInitFuncs[compInfo.ComponentId.TypeId].Func(newArch.PropertyPools[newArch.ComponentIdsMap[compInfo.ComponentId]], index);
                }
                return true;
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool SetComponent<T>(EntityId entity, T value, int variant = World.DefaultVariant) where T : struct, IComponent<T>
        {
            ValidateAliveDebug(entity);
            ref var compIndexRecord = ref GetComponentIndexRecord(entity);
            var arch = compIndexRecord.Archetype;
            var compInfo = World.GetOrCreateComponentInfo<T>(variant);
            if (HasComponent<T>(entity, variant))
            {
                ref T data = ref arch.GetComponent<T>(compIndexRecord.ArchetypeColumn, variant);
                data = value;
                return false;
            }
            else
            {
                AddComponentInternal(entity, compInfo, arch);
                return true;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool UnsetComponent<T>(EntityId entity, int variant = World.DefaultVariant) where T : struct, IComponent<T>
        {
            ValidateAliveDebug(entity);
            var arch = GetArchetype(entity);
            var compInfo = World.GetOrCreateComponentId<T>(variant);
            if (HasComponent<T>(entity, variant))
            {
                RemoveComponent<T>(entity, variant);
                return true;
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddComponent<T>(EntityId entity, int variant = World.DefaultVariant) where T : struct, IComponent<T>
        {
            ValidateAliveDebug(entity);
            var arch = GetArchetype(entity);
            var compInfo = World.GetOrCreateComponentInfo<T>(variant);
            ValidateAddDebug(arch, compInfo.ComponentId);
            (int index, Archetype newArch) = AddComponentInternal(entity, compInfo, arch);
            unsafe
            {
                OnInitFuncs[compInfo.ComponentId.TypeId].Func(newArch.PropertyPools[newArch.ComponentIdsMap[compInfo.ComponentId]], index);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddComponent<T>(EntityId entity, T value, int variant = World.DefaultVariant) where T : struct, IComponent<T>
        {
            ValidateAliveDebug(entity);
            var arch = GetArchetype(entity);
            var compInfo = World.GetOrCreateComponentInfo<T>(variant);
            ValidateAddDebug(arch, compInfo.ComponentId);
            (int index, Archetype newArch) = AddComponentInternal(entity, compInfo, arch);
            var i = GetTypeIndexRecord(newArch, compInfo.ComponentId).ComponentTypeIndex;
            ref T data = ref newArch.GetComponentByIndex<T>(index, i);
            data = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private (int index, Archetype newArch) AddComponentInternal(EntityId entity, ComponentInfo compInfo, Archetype arch)
        {
            var newArch = GetOrCreateArchetypeVariantAdd(arch, compInfo);
            //Move entity to new archetype
            //Will want to delay this in future... maybe
            var index = MoveEntityImmediate(arch, newArch, entity);

            return (index, newArch);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveComponent<T>(EntityId entity, int variant = World.DefaultVariant) where T : struct, IComponent<T>
        {
            ValidateAliveDebug(entity);
            var arch = GetArchetype(entity);
            var compInfo = World.GetOrCreateComponentId<T>(variant);
            ValidateRemoveDebug(arch, compInfo);
            RemoveComponentInternal(entity, arch, compInfo);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void RemoveComponentInternal(EntityId entity, Archetype arch, ComponentId compInfo)
        {
            var newArch = GetOrCreateArchetypeVariantRemove(arch, compInfo);
            //Move entity to new archetype
            //Will want to delay this in future... maybe
            unsafe
            {
                ref EntityIndexRecord compIndexRecord = ref GetComponentIndexRecord(entity);
                OnDeleteFuncs[compInfo.TypeId].Func(arch.PropertyPools[arch.ComponentIdsMap[compInfo]], compIndexRecord.ArchetypeColumn);
            }
            MoveEntityImmediate(arch, newArch, entity);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasComponent<T>(EntityId entity, int variant = World.DefaultVariant) where T : struct, IComponent<T>
        {
            int compInfo = GetOrCreateTypeId<T>();
            ref EntityIndexRecord record = ref GetComponentIndexRecord(entity);
            Archetype archetype = record.Archetype;
            if (TypeIndexMap.TryGetValue(new ComponentId(compInfo, variant), out var archetypes))
            {
                return archetypes.ContainsKey(archetype.Index);
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasComponent(EntityId entity, ComponentId component)
        {
            ValidateAliveDebug(entity);
            ref EntityIndexRecord record = ref GetComponentIndexRecord(entity);
            Archetype archetype = record.Archetype;

            if (TypeIndexMap.TryGetValue(component, out var archetypes))
            {
                return archetypes.ContainsKey(archetype.Index);
            }
            return false;
        }

        public ref T GetComponent<T>(EntityId entity, int variant = World.DefaultVariant) where T : struct, IComponent<T>
        {
            ValidateAliveDebug(entity);
            // First check if archetype has component
            ref var record = ref EntityIndex[entity.Id];
            var compInfo = new ComponentId(GetOrCreateTypeId<T>(), variant);
            ValidateHasDebug(record.Archetype, compInfo);
            return ref record.Archetype.GetComponent<T>(record.ArchetypeColumn, compInfo);
        }

        public void RemoveAllOfType<T>() where T : struct, IComponent<T>
        {
            var all = TypeIndexMap.Where(x => x.Key.TypeId == GetOrCreateTypeId<T>());
            foreach (var item in all)
            {
                RemoveAllWithComponent<T>(item.Key.Variant);
            }
        }

        public void RemoveAllWithComponent<T>(int variant = World.DefaultVariant) where T : struct, IComponent<T>
        {
            var archetypes = GetContainingArchetypesWithIndex(new ComponentId(GetOrCreateTypeId<T>(), variant));
            foreach (var item in archetypes)
            {
                var ents = AllArchetypes[item.Key].Entities;
                for (int i = ents.Length - 1; i >= 0; i--)
                {
                    RemoveComponent<T>(ents[i]);
                }
            }
        }

        #endregion

        #region Relation Operations
        //private ref SingleRelation GetSingleRelation<T>(EntityId entity, int variant = 0) where T : struct, ISingleRelation<T>
        //{
        //    return ref GetSingleRelationData<T>(entity, variant).GetRelation();
        //}

        private ref TreeRelation GetTreeRelation<T>(EntityId entity, int variant = 0) where T : struct, IComponent<T>
        {
            return ref GetComponent<Rel<T>>(entity, variant).TreeRelation;
        }

        internal void OnDeleteRelation<T>(ref Rel<T> rel) where T : struct, IComponent<T>
        {
            var children = rel.TreeRelation.Children;
            foreach (var child in children)
            {
                ClearParent<T>(child);
            }
        }

        //public ref T GetSingleRelationData<T>(EntityId entity, int variant = 0) where T : struct, ISingleRelation<T>
        //{
        //    ValidateAliveDebug(entity);
        //    // First check if archetype has component
        //    ref var record = ref EntityIndex[entity.Id];
        //    var compInfo = new ComponentId(GetOrCreateTypeId<T>(), variant, typeof(T));
        //    ValidateHasDebug(record.Archetype, compInfo);
        //    return ref record.Archetype.GetComponent<T>(record.ArchetypeColumn, compInfo);
        //}

        public ref T GetTreeRelationData<T>(EntityId entity, int variant = 0) where T : struct, IComponent<T>
        {
            ValidateAliveDebug(entity);
            // First check if archetype has component
            ref var record = ref EntityIndex[entity.Id];
            var compInfo = new ComponentId(GetOrCreateTypeId<T>(), variant);
            ValidateHasDebug(record.Archetype, compInfo);
            return ref record.Archetype.GetComponent<T>(record.ArchetypeColumn, compInfo);
        }

        public bool IsParentOf<T>(EntityId entity, EntityId potentialChild, int variant = 0) where T : struct, IComponent<T>
        {
            ref var relation = ref GetTreeRelation<T>(potentialChild, variant);
            return relation.parentInternal == entity;
        }

        public bool IsChildOf<T>(EntityId entity, EntityId potentialParent, int variant = 0) where T : struct, IComponent<T>
        {
            ref var relation = ref GetTreeRelation<T>(entity, variant);
            return relation.parentInternal == potentialParent;
        }

        public bool IsDecendantOf<T>(EntityId entity, EntityId potentialDecendant, int variant = 0) where T : struct, IComponent<T>
        {
            return IsAncestorOf<T>(potentialDecendant, entity, variant);
        }

        public bool IsAncestorOf<T>(EntityId entity, EntityId potentialAncestor, int variant = 0) where T : struct, IComponent<T>
        {
            ref var relation = ref GetTreeRelation<T>(entity, variant);

            if (relation.parentInternal == potentialAncestor)
            {
                return true;
            }

            if (!relation.parentInternal.HasValue)
            {
                return true;
            }

            return IsAncestorOf<T>(relation.parentInternal.Value, potentialAncestor, variant);
        }

        public void SetParent<T>(EntityId entity, EntityId parent, int variant = 0) where T : struct, IComponent<T>
        {
#if DEBUG
            if (entity == parent)
            {
                ThrowHelper.ThrowArgumentException("Tried to set itself as value parentInternal entity");
            }

            if (IsDecendantOf<T>(entity, parent))
            {
                ThrowHelper.ThrowArgumentException("Tried to set value decendant as the parentInternal of this entity");
            }
#endif
            ref var relation = ref GetTreeRelation<T>(entity, variant);
            if (relation.parentInternal.HasValue)
            {
                ref var relation2 = ref GetTreeRelation<T>(relation.parentInternal.Value, variant);
                relation2.RemoveChild(entity);
            }
            relation.parentInternal = parent;
            ref var relation3 = ref GetTreeRelation<T>(parent, variant);
            relation3.AddChild(entity);
        }

        public void AddChild<T>(EntityId entity, EntityId child, int variant = 0) where T : struct, IComponent<T>
        {
#if DEBUG
            if (entity == child)
            {
                ThrowHelper.ThrowArgumentException("Tried to add itself as value child entity");
            }

            if (IsAncestorOf<T>(entity, child))
            {
                ThrowHelper.ThrowArgumentException("Tried to add value child that is already an ancestor of this relation");
            }
#endif
            ref var relation = ref GetTreeRelation<T>(entity, variant);
            relation.AddChild(child);
            ref var relation2 = ref GetTreeRelation<T>(child, variant);
            if (relation2.parentInternal.HasValue)
            {
                ref var relation3 = ref GetTreeRelation<T>(relation2.parentInternal.Value, variant);
                relation3.RemoveChild(child);
            }
            relation2.parentInternal = entity;
        }

        public void RemoveChild<T>(EntityId entity, EntityId child, int variant = 0) where T : struct, IComponent<T>
        {
            ref var relation = ref GetTreeRelation<T>(entity, variant);
            relation.RemoveChild(child);
            ref var relation2 = ref GetTreeRelation<T>(child, variant);
            relation2.parentInternal = null;
        }

        public void ClearParent<T>(EntityId entity, int variant = 0) where T : struct, IComponent<T>
        {
            ref var relation = ref GetTreeRelation<T>(entity, variant);
            if (relation.parentInternal.HasValue)
            {
                ref var relation2 = ref GetTreeRelation<T>(relation.parentInternal.Value, variant);
                relation2.RemoveChild(entity);
                relation.parentInternal = null;
            }
        }

        public ReadOnlySpan<EntityId> GetChildren<T>(EntityId entity, int variant = 0) where T : struct, IComponent<T>
        {
            return GetTreeRelation<T>(entity, variant).Children;
        }

        public EntityId? GetParent<T>(EntityId entity, int variant = 0) where T : struct, IComponent<T>
        {
            return GetTreeRelation<T>(entity, variant).parentInternal;
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
        private Archetype GetOrCreateArchetypeVariantAdd(Archetype source, ComponentInfo compInfo)
        {
            Archetype? archetype;
            //Archetype already stored in graph
            if (source.TryGetSiblingAdd(compInfo.ComponentId, out archetype))
            {
                return archetype;
            }
            int length = source.ComponentInfo.Length + 1;
            ComponentInfo[] pool = ArrayPool<ComponentInfo>.Shared.Rent(length);
            for (int i = 0; i < source.ComponentInfo.Length; i++)
            {
                pool[i] = source.ComponentInfo[i];
            }
            pool[length - 1] = compInfo;
            var span = pool.AsSpan(0, length);
            int hash = GetComponentHash(span);
            archetype = GetArchetypeByHashCode(hash);
            //We found it!
            if (archetype != null)
            {
                ArrayPool<ComponentInfo>.Shared.Return(pool);
                return archetype;
            }
            //Archetype does not yet exist, create it!
            var definition = new ArchetypeDefinition(hash, span.ToArray());
            archetype = CreateArchetype(definition);
            ArrayPool<ComponentInfo>.Shared.Return(pool);
            source.SetSiblingAdd(compInfo.ComponentId, archetype);
            return archetype;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Archetype GetOrCreateArchetypeVariantRemove(Archetype source, ComponentId compInfo)
        {
            //Archetype already stored in graph
            if (source.TryGetSiblingRemove(compInfo, out var a))
            {
                return a;
            }
            //Graph failed we need to find Archetype by hash
            int length = source.ComponentInfo.Length - 1;
            ComponentInfo[] pool = ArrayPool<ComponentInfo>.Shared.Rent(length);
            int index = 0;
            for (int i = 0; i < source.ComponentInfo.Length; i++)
            {
                var compPool = source.ComponentInfo[i];
                if (compPool != compInfo)
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
                ArrayPool<ComponentInfo>.Shared.Return(pool);
                return arch;
            }
            //Archetype does not yet exist, create it!
            var definition = new ArchetypeDefinition(hash, span.ToArray());
            var archetype = CreateArchetype(definition);
            ArrayPool<ComponentInfo>.Shared.Return(pool);
            source.SetSiblingRemove(compInfo, archetype);
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
                int id = definition.ComponentIds[i].ComponentId.TypeId;
                mask.SetBit(id);
            }
            var archetype = new Archetype(definition.ComponentIds, mask, definition.HashCode, archetypeCount);
            // Store in index
            for (int i = 0; i < compIds.Length; i++)
            {
                ref var dict = ref CollectionsMarshal.GetValueRefOrAddDefault(TypeIndexMap, compIds[i].ComponentId, out var exists);
                if (!exists)
                {
                    dict = new Dictionary<int, TypeIndexRecord>();
                }
                dict!.Add(archetype.Index, new TypeIndexRecord(i));
            }
            // Store in all variantMap
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
            var filter = new EntityFilter(this, mask.IncludeMask, mask.ExcludeMask);
            AllFilters = AllFilters.GrowIfNeeded(filterCount, 1);
            AllFilters[filterCount] = filter;
            filterId = filterCount++;
            return filter;
        }

        //See Core/Queries.cs for Queries
        #endregion


        internal void Lock()
        {
            isLocked = true;
        }

        internal void Unlock()
        {
            isLocked = false;
        }

        public void Dispose()
        {
            entityCounter = 0;
            worlds[WorldId] = null!;
            recycledWorlds.Add(WorldId);
        }

        public void Reset()
        {
            FilterMap.Clear();
            TypeIndexMap.Clear();
            ArchetypeIndexMap.Clear();
            Archetypes.Clear();
            Filters.Clear();
            EntityIndices.Clear();
            entityCounter = 0;
            componentCounter = 0;
            archetypeCount = 0;
            filterCount = 0;
        }

        public static void GlobalReset()
        {
            for (int i = 0; i < worldCounter; i++)
            {
                worlds[i]?.Reset();
            }
            TypeMap.Clear();
            TypeMapReverse.Clear();
        }
    }
}

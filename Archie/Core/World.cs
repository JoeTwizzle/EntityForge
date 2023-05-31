using Archie.Collections;
using Archie.Collections.Generic;
using Archie.Commands;
using Archie.Helpers;
using Archie.Queries;
using Archie.Relations;
using CommunityToolkit.HighPerformance;
using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Runtime;
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

        public static ReadOnlySpan<World> Worlds => new ReadOnlySpan<World>(worlds, 0, worldCounter);
        public const int DefaultComponents = 16;
        public const int DefaultEntities = 256;
        public const int DefaultVariant = 0;
        internal static readonly ArchetypeDefinition EmptyArchetypeDefinition = new ArchetypeDefinition(GetComponentHash(Array.Empty<ComponentInfo>()), Array.Empty<ComponentInfo>());
        private static readonly List<int> recycledWorlds = new();
        private static readonly ReaderWriterLockSlim createTypeRWLock = new();
        private static readonly object createWorldLock = new object();
        private static World[] worlds = new World[1];
        private static int worldCounter;
        private static int componentCounter;
        /// <summary>
        /// Stores the meta item id has
        /// </summary>
        private static readonly Dictionary<Type, ComponentMetaData> TypeMap = new();
        /// <summary>
        /// Stores the type an meta has
        /// </summary>
        private static readonly Dictionary<int, Type> TypeMapReverse = new();

        /// <summary>
        /// Stores in which archetype an entityId is
        /// </summary>
        private EntityIndexRecord[] EntityIndex;
        /// <summary>
        /// Stores item filter based on the hash of item ComponentMask
        /// </summary>
        private readonly Dictionary<ComponentMask, int> FilterMap;
        /// <summary>
        /// Maps an Archetype's Definition to its index
        /// </summary>
        private readonly Dictionary<ArchetypeDefinition, int> ArchetypeIndexMap;
        /// <summary>
        /// Used to find the variantMap containing item componentInfo and its index
        /// </summary>
        private readonly Dictionary<ComponentId, Dictionary<int, TypeIndexRecord>> TypeIndexMap;
        /// <summary>
        /// Contains now deleted Entities whoose ids may be reused
        /// </summary>
        private EntityId[] RecycledEntities;
        internal readonly ReaderWriterLockSlim worldEntitiesRWLock = new();
        internal readonly ReaderWriterLockSlim worldArchetypesRWLock = new();
        internal readonly ReaderWriterLockSlim worldFilterRWLock = new();
        /// <summary>
        /// Stores all variantMap by their creation meta
        /// </summary>
        internal Archetype[] AllArchetypes;
        /// <summary>
        /// Stores all filters by their creation meta
        /// </summary>
        internal EntityFilter[] AllFilters;
        /// <summary>
        /// The meta of this World
        /// </summary>
        public int WorldId { get; private init; }
        /// <summary>
        /// Number of different archtypes in this World
        /// </summary>
        public int ArchtypeCount => archetypeCount;
        /// <summary>
        /// Span of all EntitiesPool currently present in the World
        /// </summary>
        public ReadOnlySpan<EntityIndexRecord> EntityIndices => new ReadOnlySpan<EntityIndexRecord>(EntityIndex, 0, entityCounter);
        /// <summary>
        /// Span of all variantMap currently present in the World
        /// </summary>
        public ReadOnlySpan<Archetype> Archetypes => new ReadOnlySpan<Archetype>(AllArchetypes, 0, archetypeCount);
        /// <summary>
        /// Span of all filters currently present in the World
        /// </summary>
        public ReadOnlySpan<EntityFilter> Filters => new ReadOnlySpan<EntityFilter>(AllFilters, 0, filterCount);
        int filterCount;
        int archetypeCount;
        int entityCounter;
        int recycledEntitiesCount;

        public World()
        {
            lock (createWorldLock)
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
                //Create entityId with meta 0 and mark it as Destroyed
                //We do this so default(entityId) is not a valid entityId
                ref var idx = ref EntityIndex[CreateEntity().Id];
                idx.EntityVersion = (short)-1;
                idx.Archetype.ElementCount--;
            }
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
        public static int GetTypeId(Type type)
        {
            createTypeRWLock.EnterReadLock();
            var id = TypeMap[type].Id;
            createTypeRWLock.ExitReadLock();
            return id;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ComponentMetaData GetTypeMetaData(Type type)
        {
            createTypeRWLock.EnterReadLock();
            var meta = TypeMap[type];
            createTypeRWLock.ExitReadLock();
            return meta;
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

        private static void CreateTypeId<T>() where T : struct, IComponent<T>
        {
            createTypeRWLock.EnterWriteLock();
            ref var metaData = ref CollectionsMarshal.GetValueRefOrAddDefault(TypeMap, typeof(T), out var exists);
            if (!exists)
            {
                metaData.Id = componentCounter++;
                TypeMapReverse.Add(metaData.Id, typeof(T));
                T.Registered = true;
                metaData.Type = typeof(T);
                metaData.UnmanagedSize = Unsafe.SizeOf<T>();
                T.Id = metaData.Id;
            }
            createTypeRWLock.ExitWriteLock();
        }

        public static ComponentId GetOrCreateComponentId<T>(int variant = 0) where T : struct, IComponent<T>
        {
            return new ComponentId(GetOrCreateTypeId<T>(), variant);
        }

        public static ComponentInfo GetOrCreateComponentInfo<T>(int variant = 0) where T : struct, IComponent<T>
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
                    //This only works if the sparseArray is sorted
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
                    //This only works if the sparseArray is sorted
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
            return GetEntityIndexRecord(entity).Archetype;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public ref EntityIndexRecord GetEntityIndexRecord(EntityId entity)
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
                ThrowHelper.ThrowArgumentException($"Tried accessing destroyed entityId: {entity}");
            }
        }

        [Conditional("DEBUG")]
        private void ValidateDestroyedDebug(EntityId entity)
        {
            if (IsAlive(entity))
            {
                ThrowHelper.ThrowArgumentException($"Tried accessing alive entityId: {entity}");
            }
        }

        #endregion

        #region Entity Operations

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ReserveEntities(in ArchetypeDefinition definition, int count)
        {
            var archetype = GetOrCreateArchetype(definition);
            if (archetype.IsLocked)
            {
                return;
            }
            archetype.GrowBy(count);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Entity CreateEntity()
        {
            return CreateEntity(EmptyArchetypeDefinition);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Entity CreateEntity(in ArchetypeDefinition definition)
        {
            var archetype = GetOrCreateArchetype(definition);
            EntityId entityId;
            if (recycledEntitiesCount > 0)
            {
                entityId = RecycledEntities[--recycledEntitiesCount];
            }
            else
            {
                EntityIndex = EntityIndex.GrowIfNeeded(entityCounter, 1);
                entityId = new EntityId(entityCounter++);
                EntityIndex[entityId.Id].EntityVersion = -1;
            }
            var entity = new Entity(entityId.Id, WorldId);
            ref var entIndex = ref EntityIndex[entityId.Id];
            entIndex.Archetype = archetype;
            entIndex.EntityVersion = (short)-entIndex.EntityVersion;
            entIndex.ArchetypeColumn = archetype.ElementCount;
            if (archetype.IsLocked)
            {
                entIndex.ArchetypeColumn = archetype.CommandBuffer.Create(archetype.ElementCount, entityId);
                return entity;
            }
            archetype.AddEntityInternal(entity);
            return entity;
        }

        public void DestroyEntity(EntityId entityId)
        {
            ValidateAliveDebug(entityId);

            ref EntityIndexRecord compIndexRecord = ref GetEntityIndexRecord(entityId);
            var src = compIndexRecord.Archetype;
            //Get index of entityId to be removed
            ref var entityIndex = ref EntityIndex[entityId.Id];
            //Set its version to its negative increment (Mark entityId as destroyed)
            entityIndex.EntityVersion = (short)-(entityIndex.EntityVersion + 1);

            RecycledEntities = RecycledEntities.GrowIfNeeded(recycledEntitiesCount, 1);
            RecycledEntities[recycledEntitiesCount++] = entityId;
            if (src.IsLocked)
            {
                src.CommandBuffer.Destroy(entityIndex.ArchetypeColumn);
                return;
            }

            DeleteEntityInternal(src, compIndexRecord.ArchetypeColumn);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void DeleteEntityInternal(Archetype src, int oldIndex)
        {
            //Fill hole in id sparseArray
            src.FillHole(oldIndex);
            //Update index of entityId filling the hole
            ref EntityIndexRecord rec = ref GetEntityIndexRecord(src.Entities[--src.ElementCount]);
            rec.ArchetypeColumn = oldIndex;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Entity GetEntity(EntityId id)
        {
            return new Entity(id.Id, WorldId);
        }

        internal int MoveEntity(Archetype src, Archetype dest, EntityId entity)
        {
            Debug.Assert(src.Index != dest.Index);

            ref EntityIndexRecord compIndexRecord = ref GetEntityIndexRecord(entity);
            int oldIndex = compIndexRecord.ArchetypeColumn;

            dest.GrowBy(1);
            dest.EntitiesPool.GetRefAt(dest.ElementCount) = new Entity(entity.Id, WorldId);
            int newIndex = dest.ElementCount++;
            //Copy Pool to new Arrays
            src.CopyComponents(oldIndex, dest, newIndex);
            //Fill hole in old Arrays
            src.FillHole(oldIndex);

            //Update index of entityId filling the hole
            ref EntityIndexRecord rec = ref GetEntityIndexRecord(src.Entities[src.ElementCount - 1]);
            rec.ArchetypeColumn = oldIndex;
            //Update index of moved entityId
            compIndexRecord.ArchetypeColumn = newIndex;
            compIndexRecord.Archetype = dest;
            //Finish removing entityId from source
            src.ElementCount--;
            return newIndex;
        }

        #endregion

        #region Component Operations

        #region Internal

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void AddComponentInternal(EntityId entity, ComponentInfo info, Archetype arch)
        {
            if (arch.IsLocked)
            {
                ref EntityIndexRecord entityIndex = ref GetEntityIndexRecord(entity);
                var storedArch = arch.CommandBuffer.GetArchetype(entityIndex.ArchetypeColumn);
                //Check if we have a pending move already scheduled
                Archetype newArch;
                if (storedArch != null)
                {
                    newArch = GetOrCreateArchetypeVariantAdd(storedArch, info);
                }
                else
                {
                    newArch = GetOrCreateArchetypeVariantAdd(arch, info);
                }
                arch.CommandBuffer.Move(entityIndex.ArchetypeColumn, newArch, entity);
            }
            else
            {
                var newArch = GetOrCreateArchetypeVariantAdd(arch, info);
                MoveEntity(arch, newArch, entity);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void RemoveComponentInternal(EntityId entity, Archetype arch, ComponentInfo info)
        {
            if (arch.IsLocked)
            {
                ref EntityIndexRecord entityIndex = ref GetEntityIndexRecord(entity);
                var storedArch = arch.CommandBuffer.GetArchetype(entityIndex.ArchetypeColumn);
                //Check if we have a pending move already scheduled
                Archetype newArch;
                if (storedArch != null)
                {
                    newArch = GetOrCreateArchetypeVariantRemove(storedArch, info.ComponentId);
                }
                else
                {
                    newArch = GetOrCreateArchetypeVariantRemove(arch, info.ComponentId);
                }
                arch.CommandBuffer.Move(entityIndex.ArchetypeColumn, newArch, entity);
                arch.CommandBuffer.UnsetValue(entityIndex.ArchetypeColumn, info);
            }
            else
            {
                var newArch = GetOrCreateArchetypeVariantRemove(arch, info.ComponentId);
                MoveEntity(arch, newArch, entity);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void SetValues(EntityId entity, UnsafeSparseSet<UnsafeSparseSet> valuesSetSet)
        {
            ref var entityIndex = ref GetEntityIndexRecord(entity);
            var archetype = entityIndex.Archetype;

            var valueSets = valuesSetSet.GetDenseData();
            var valueIndices = valuesSetSet.GetIndexData();
            var infos = archetype.ComponentInfo.Span;
            for (int i = 0; i < valueSets.Length; i++)
            {
                var typeId = valueIndices[i];
                var setBundle = valueSets[i];
                if (archetype.ComponentIdsMap.TryGetValue(new ComponentId(typeId, 0), out var index))
                {
                    if (setBundle.TryGetIndex(entity.Id, out var denseIndex))
                    {
                        var compInfo = infos[index];
                        if (compInfo.IsUnmanaged)
                        {
                            unsafe
                            {
                                setBundle.CopyToUnmanagedRaw(denseIndex, archetype.ComponentPools[index].UnmanagedData, entityIndex.ArchetypeColumn, compInfo.UnmanagedSize);
                            }
                        }
                        else
                        {
                            setBundle.CopyToManagedRaw(denseIndex, archetype.ComponentPools[index].ManagedData!, entityIndex.ArchetypeColumn, 1);
                        }
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void AddComponentWithValueInternal<T>(EntityId entity, T value, Archetype arch) where T : struct, IComponent<T>
        {
            var info = GetOrCreateComponentInfo<T>();
            if (arch.IsLocked)
            {
                ref EntityIndexRecord entityIndex = ref GetEntityIndexRecord(entity);
                var storedArch = arch.CommandBuffer.GetArchetype(entityIndex.ArchetypeColumn);
                //Check if we have a pending move already scheduled
                Archetype newArch;
                if (storedArch != null)
                {
                    newArch = GetOrCreateArchetypeVariantAdd(storedArch, info);
                }
                else
                {
                    newArch = GetOrCreateArchetypeVariantAdd(arch, info);
                }
                arch.CommandBuffer.Move(entityIndex.ArchetypeColumn, newArch, entity);
                arch.CommandBuffer.SetValue(entityIndex.ArchetypeColumn, value);
            }
            else
            {
                var newArch = GetOrCreateArchetypeVariantAdd(arch, info);
                var index = MoveEntity(arch, newArch, entity);
                var i = GetTypeIndexRecord(newArch, info.ComponentId).ComponentTypeIndex;
                ref T data = ref newArch.GetComponentByIndex<T>(index, i);
                data = value;
            }
        }

        #endregion

        #region Typeless

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasComponent(EntityId entity, Type type, int variant = World.DefaultVariant)
        {
            return HasComponent(entity, new ComponentId(GetTypeId(type), variant));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasComponent(EntityId entity, ComponentId component)
        {
            ValidateAliveDebug(entity);
            ref EntityIndexRecord record = ref GetEntityIndexRecord(entity);
            Archetype archetype = record.Archetype;

            if (TypeIndexMap.TryGetValue(component, out var archetypes))
            {
                return archetypes.ContainsKey(archetype.Index);
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddComponent(EntityId entity, Type type, int variant = World.DefaultVariant)
        {
            var meta = GetTypeMetaData(type);
            var compId = new ComponentId(meta.Id, variant);
            var componentInfo = meta.IsUnmanaged ? new ComponentInfo(compId, meta.UnmanagedSize) : new ComponentInfo(compId, meta.Type);
            AddComponent(entity, componentInfo);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddComponent(EntityId entity, ComponentInfo compInfo)
        {
            ValidateAliveDebug(entity);
            var arch = GetArchetype(entity);
            ValidateAddDebug(arch, compInfo.ComponentId);
            AddComponentInternal(entity, compInfo, arch);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveComponent(EntityId entity, Type type, int variant = World.DefaultVariant)
        {
            if (TypeMap.TryGetValue(type, out var meta))
            {
                RemoveComponent(entity, new ComponentInfo(new ComponentId(meta.Id, variant), meta.UnmanagedSize));
            }
            else
            {
                throw new InvalidOperationException("Tried to remove unregistered component");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveComponent(EntityId entity, ComponentInfo componentId)
        {
            ValidateAliveDebug(entity);
            var arch = GetArchetype(entity);
            ValidateRemoveDebug(arch, componentId.ComponentId);
            RemoveComponentInternal(entity, arch, componentId);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool SetComponent(EntityId entity, Type type, int variant = World.DefaultVariant)
        {
            var meta = GetTypeMetaData(type);
            var compId = new ComponentId(meta.Id, variant);
            return SetComponent(entity, meta.IsUnmanaged ? new ComponentInfo(compId, meta.UnmanagedSize) : new ComponentInfo(compId, meta.Type));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool SetComponent(EntityId entity, ComponentInfo compInfo)
        {
            ref var compIndexRecord = ref GetEntityIndexRecord(entity);
            var arch = compIndexRecord.Archetype;

            if (!HasComponent(entity, compInfo.ComponentId))
            {
                AddComponentInternal(entity, compInfo, arch);
                return true;
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool UnsetComponent(EntityId entity, Type component, int variant = World.DefaultVariant)
        {
            if (TypeMap.TryGetValue(component, out var meta))
            {
                return UnsetComponent(entity, new ComponentInfo(new ComponentId(meta.Id, variant), meta.UnmanagedSize));
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool UnsetComponent(EntityId entity, ComponentInfo info)
        {
            ValidateAliveDebug(entity);
            var arch = GetArchetype(entity);
            if (HasComponent(entity, info.ComponentId))
            {
                RemoveComponent(entity, info);
                return true;
            }
            return false;
        }

        #endregion

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public bool SetComponent(EntityId entity, object value, int variant = World.DefaultVariant)
        //{
        //    ValidateAliveDebug(entity);
        //    var type = value.GetType();
        //    ref var compIndexRecord = ref GetEntityIndexRecord(entity);
        //    var arch = compIndexRecord.Archetype;
        //    var meta = GetTypeMetaData(type);
        //    var compId = new ComponentId(meta.Id, variant);
        //    var componentInfo = meta.IsUnmanaged ? new ComponentInfo(compId, meta.UnmanagedSize) : new ComponentInfo(compId, meta.Type);
        //    if (HasComponent(entity, componentInfo.ComponentId))
        //    {
        //        if (arch.IsLocked)
        //        {

        //            return false;
        //        }
        //        arch.SetComponent(compIndexRecord.ArchetypeColumn, componentInfo, value);
        //        return false;
        //    }
        //    else
        //    {
        //        AddComponentInternal(entity, componentInfo, arch);
        //        if (arch.IsLocked)
        //        {
        //            var dest = GetOrCreateArchetypeVariantAdd(arch, componentInfo);
        //            return false;
        //        }
        //        return true;
        //    }
        //}

        #region Generic

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool SetComponent<T>(EntityId entity, int variant = World.DefaultVariant) where T : struct, IComponent<T>
        {
            ref var compIndexRecord = ref GetEntityIndexRecord(entity);
            var arch = compIndexRecord.Archetype;
            var compInfo = World.GetOrCreateComponentInfo<T>(variant);

            if (!HasComponent<T>(entity, variant))
            {
                AddComponentInternal(entity, compInfo, arch);
                return true;
            }
            return false;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool SetComponent<T>(EntityId entity, T value, int variant = World.DefaultVariant) where T : struct, IComponent<T>
        {
            ValidateAliveDebug(entity);
            ref var compIndexRecord = ref GetEntityIndexRecord(entity);
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
                AddComponentWithValueInternal(entity, value, arch);
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
            AddComponentInternal(entity, compInfo, arch);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddComponent<T>(EntityId entity, T value, int variant = World.DefaultVariant) where T : struct, IComponent<T>
        {
            ValidateAliveDebug(entity);
            var arch = GetArchetype(entity);
            var compInfo = World.GetOrCreateComponentInfo<T>(variant);
            ValidateAddDebug(arch, compInfo.ComponentId);
            AddComponentWithValueInternal(entity, value, arch);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveComponent<T>(EntityId entity, int variant = World.DefaultVariant) where T : struct, IComponent<T>
        {
            ValidateAliveDebug(entity);
            var arch = GetArchetype(entity);
            var compInfo = World.GetOrCreateComponentInfo<T>(variant);
            ValidateRemoveDebug(arch, compInfo.ComponentId);
            RemoveComponentInternal(entity, arch, compInfo);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasComponent<T>(EntityId entity, int variant = World.DefaultVariant) where T : struct, IComponent<T>
        {
            int compInfo = GetOrCreateTypeId<T>();
            ref EntityIndexRecord record = ref GetEntityIndexRecord(entity);
            Archetype archetype = record.Archetype;
            if (TypeIndexMap.TryGetValue(new ComponentId(compInfo, variant), out var archetypes))
            {
                return archetypes.ContainsKey(archetype.Index);
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T GetComponent<T>(EntityId entity, int variant = World.DefaultVariant) where T : struct, IComponent<T>
        {
            ValidateAliveDebug(entity);
            // First check if archetype has id
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
                var arch = AllArchetypes[item.Key];

                var ents = arch.Entities;
                for (int i = ents.Length - 1; i >= 0; i--)
                {
                    if (arch.IsLocked)
                    {
                        //defList.RemoveOp(ents[i], arch, GetOrCreateArchetypeVariantRemove(arch, new ComponentId(GetOrCreateTypeId<T>(), variant)));
                        continue;
                    }
                    RemoveComponent<T>(ents[i]);
                }
            }
        }

        #endregion

        #endregion

        #region Relation Operations
        //private ref SingleRelation GetSingleRelation<T>(EntityId entityId, int variant = 0) where T : struct, ISingleRelation<T>
        //{
        //    return ref GetSingleRelationData<T>(entityId, variant).GetRelation();
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

        //public ref T GetSingleRelationData<T>(EntityId entityId, int variant = 0) where T : struct, ISingleRelation<T>
        //{
        //    ValidateAliveDebug(entityId);
        //    // First check if archetype has id
        //    ref var record = ref EntityIndex[entityId.Id];
        //    var meta = new ComponentId(GetOrCreateTypeId<T>(), variant, typeof(T));
        //    ValidateHasDebug(record.Archetype, meta);
        //    return ref record.Archetype.GetComponent<T>(record.ArchetypeColumn, meta);
        //}

        public ref T GetTreeRelationData<T>(EntityId entity, int variant = 0) where T : struct, IComponent<T>
        {
            ValidateAliveDebug(entity);
            // First check if archetype has id
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
                ThrowHelper.ThrowArgumentException("Tried to set itself as item parentInternal entity");
            }

            if (IsDecendantOf<T>(entity, parent))
            {
                ThrowHelper.ThrowArgumentException("Tried to set item decendant as the parentInternal of this entity");
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
                ThrowHelper.ThrowArgumentException("Tried to add itself as item child entity");
            }

            if (IsAncestorOf<T>(entity, child))
            {
                ThrowHelper.ThrowArgumentException("Tried to add item child that is already an ancestor of this relation");
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
            worldArchetypesRWLock.EnterReadLock();
            if (ArchetypeIndexMap.TryGetValue(definition, out var index))
            {
                worldArchetypesRWLock.ExitReadLock();
                return AllArchetypes[index];
            }
            worldArchetypesRWLock.ExitReadLock();
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
            var infos = source.ComponentInfo.Span;
            for (int i = 0; i < source.ComponentInfo.Length; i++)
            {
                pool[i] = infos[i];
            }
            pool[length - 1] = compInfo;
            var memory = pool.AsMemory(0, length);
            int hash = GetComponentHash(memory.Span);
            archetype = GetArchetype(new ArchetypeDefinition(hash, memory));
            //We found it!
            if (archetype != null)
            {
                ArrayPool<ComponentInfo>.Shared.Return(pool);
                source.SetSiblingAdd(compInfo.ComponentId, archetype);
                return archetype;
            }
            //Archetype does not yet exist, create it!
            var definition = new ArchetypeDefinition(hash, memory.ToArray());
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
            var infos = source.ComponentInfo.Span;
            for (int i = 0; i < source.ComponentInfo.Length; i++)
            {
                var compPool = infos[i];
                if (compPool.ComponentId != compInfo)
                {
                    pool[index++] = compPool;
                }
            }
            var memory = pool.AsMemory(0, length);
            int hash = GetComponentHash(memory.Span);
            var archetype = GetArchetype(new ArchetypeDefinition(hash, memory));
            //We found it!
            if (archetype != null)
            {
                ArrayPool<ComponentInfo>.Shared.Return(pool);
                source.SetSiblingRemove(compInfo, archetype);
                return archetype;
            }
            //Archetype does not yet exist, create it!
            var definition = new ArchetypeDefinition(hash, memory.ToArray());
            archetype = CreateArchetype(definition);
            ArrayPool<ComponentInfo>.Shared.Return(pool);
            source.SetSiblingRemove(compInfo, archetype);
            return archetype;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Archetype CreateArchetype(in ArchetypeDefinition definition)
        {
            //Store type Definitions
            var mask = new BitMask();
            var infos = definition.ComponentInfos.Span;
            for (int i = 0; i < definition.ComponentInfos.Length; i++)
            {
                int id = infos[i].ComponentId.TypeId;
                mask.SetBit(id);
            }
            var archetype = new Archetype(this, definition.ComponentInfos, mask, definition.HashCode, archetypeCount);
            worldArchetypesRWLock.EnterWriteLock();
            // Store in index
            for (int i = 0; i < infos.Length; i++)
            {
                ref var dict = ref CollectionsMarshal.GetValueRefOrAddDefault(TypeIndexMap, infos[i].ComponentId, out var exists);
                if (!exists)
                {
                    dict = new Dictionary<int, TypeIndexRecord>();
                }
                dict!.Add(archetype.Index, new TypeIndexRecord(i));
            }
            // Store in all variantMap
            ArchetypeIndexMap.Add(definition, archetypeCount);
            AllArchetypes = AllArchetypes.GrowIfNeeded(archetypeCount, 1);
            AllArchetypes[archetypeCount++] = archetype;
            for (int i = 0; i < filterCount; i++)
            {
                AllFilters[i].Update(archetype);
            }
            worldArchetypesRWLock.ExitWriteLock();
            return archetype;
        }

        #endregion

        #region Filters

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityFilter GetFilter(ComponentMask mask)
        {
            worldFilterRWLock.EnterUpgradeableReadLock();
            ref var filterId = ref CollectionsMarshal.GetValueRefOrAddDefault(FilterMap, mask, out bool exists);
            if (exists)
            {
                worldFilterRWLock.ExitUpgradeableReadLock();
                return AllFilters[filterId];
            }
            var filter = new EntityFilter(this, mask.HasMask, mask.ExcludeMask, mask.SomeMasks);
            worldFilterRWLock.EnterWriteLock();
            AllFilters = AllFilters.GrowIfNeeded(filterCount, 1);
            AllFilters[filterCount] = filter;
            filterId = filterCount++;
            worldFilterRWLock.ExitWriteLock();
            return filter;
        }

        //See Core/Queries.cs for Queries
        #endregion

        public void Dispose()
        {
            entityCounter = 0;
            worlds[WorldId] = null!;
            worldEntitiesRWLock.Dispose();
            worldFilterRWLock.Dispose();
            worldArchetypesRWLock.Dispose();
            recycledWorlds.Add(WorldId);
        }

        public void Reset()
        {
            FilterMap.Clear();
            TypeIndexMap.Clear();
            ArchetypeIndexMap.Clear();
            AllArchetypes.AsSpan(0, archetypeCount).Clear();
            AllFilters.AsSpan(0, filterCount).Clear();
            EntityIndex.AsSpan(0, entityCounter).Clear();
            RecycledEntities.AsSpan(0, recycledEntitiesCount).Clear();
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

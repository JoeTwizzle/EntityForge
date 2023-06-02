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
using System.Security.AccessControl;

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
        private readonly Dictionary<int, Dictionary<int, TypeIndexRecord>> TypeIndexMap;
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
                ref var idx = ref EntityIndex[CreateEntity().EntityId.Id];
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
            var id = TypeMap[type].TypeId;
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
                metaData.TypeId = componentCounter++;
                TypeMapReverse.Add(metaData.TypeId, typeof(T));
                T.Registered = true;
                metaData.Type = typeof(T);
                metaData.UnmanagedSize = Unsafe.SizeOf<T>();
                T.Id = metaData.TypeId;
            }
            createTypeRWLock.ExitWriteLock();
        }

        public static ComponentInfo GetOrCreateComponentInfo<T>() where T : struct, IComponent<T>
        {
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                return new ComponentInfo(GetOrCreateTypeId<T>(), typeof(T));
            }
            else
            {
                return new ComponentInfo(GetOrCreateTypeId<T>(), Unsafe.SizeOf<T>());
            }
        }

        public static void SortTypes(Span<int> componentTypes)
        {
            componentTypes.Sort((x, y) =>
            {
                int val = x > y ? 1 : (x < y ? -1 : 0);
                return val;
            });
        }

        public static int[] RemoveDuplicates(int[] types)
        {
            int head = 0;
            Span<int> indices = types.Length < 32 ? stackalloc int[32] : new int[types.Length];
            if (types.Length > 0)
            {
                int prevType = types[0];
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
            var deDup = new int[head];
            for (int i = 0; i < deDup.Length; i++)
            {
                deDup[i] = types[indices[--head]];
            }
            return deDup;
        }

        public static int GetComponentHash(Span<int> componentTypes)
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
                return x.TypeId > y.TypeId ? 1 : (x.TypeId < y.TypeId ? -1 : 0);
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref EntityIndexRecord GetEntityIndexRecord(EntityId entity)
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
        public ref TypeIndexRecord GetTypeIndexRecord(Archetype archetype, int typeId)
        {
            return ref GetContainingArchetypesWithIndex(typeId).Get(archetype.Index);
        }
        #endregion

        #region Debug Checks

        [Conditional("DEBUG")]
        private static void ValidateHasDebug(Archetype archetype, int type)
        {
            if (!archetype.HasComponent(type))
            {
                ThrowHelper.ThrowDuplicateComponentException($"Tried adding duplicate Component of type {type}");
            }
        }

        [Conditional("DEBUG")]
        private static void ValidateAddDebug(Archetype archetype, int type)
        {
            if (archetype.HasComponent(type))
            {
                ThrowHelper.ThrowDuplicateComponentException($"Tried adding duplicate Component of type {type}");
            }
        }

        [Conditional("DEBUG")]
        private static void ValidateRemoveDebug(Archetype archetype, int type)
        {
            if (!archetype.HasComponent(type))
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
                    newArch = GetOrCreateArchetypeVariantRemove(storedArch, info.TypeId);
                }
                else
                {
                    newArch = GetOrCreateArchetypeVariantRemove(arch, info.TypeId);
                }
                ValidateRemoveDebug(storedArch ?? arch, info.TypeId);
                arch.CommandBuffer.Move(entityIndex.ArchetypeColumn, newArch, entity);
                arch.CommandBuffer.UnsetValue(entityIndex.ArchetypeColumn, info);
            }
            else
            {
                ValidateRemoveDebug(arch, info.TypeId);
                var newArch = GetOrCreateArchetypeVariantRemove(arch, info.TypeId);
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
                if (archetype.ComponentIdsMap.TryGetValue(typeId, out var index))
                {
                    if (setBundle.TryGetIndex(entityIndex.ArchetypeColumn, out var denseIndex))
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
                var i = GetTypeIndexRecord(newArch, info.TypeId).ComponentTypeIndex;
                ref T data = ref newArch.GetComponentByIndex<T>(index, i);
                data = value;
            }
        }

        #endregion

        #region Typeless

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasComponent(EntityId entity, Type type)
        {
            return HasComponent(entity, GetTypeId(type));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasComponent(EntityId entity, int component)
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
        public void AddComponent(EntityId entity, Type type)
        {
            var meta = GetTypeMetaData(type);
            var compId = meta.TypeId;
            var componentInfo = meta.IsUnmanaged ? new ComponentInfo(compId, meta.UnmanagedSize) : new ComponentInfo(compId, meta.Type);
            AddComponent(entity, componentInfo);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddComponent(EntityId entity, ComponentInfo compInfo)
        {
            ValidateAliveDebug(entity);
            var arch = GetArchetype(entity);
            ValidateAddDebug(arch, compInfo.TypeId);
            AddComponentInternal(entity, compInfo, arch);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveComponent(EntityId entity, Type type)
        {
            if (TypeMap.TryGetValue(type, out var meta))
            {
                RemoveComponent(entity, new ComponentInfo(meta.TypeId, meta.UnmanagedSize));
            }
            else
            {
                throw new InvalidOperationException("Tried to remove unregistered component");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveComponent(EntityId entity, ComponentInfo typeId)
        {
            ValidateAliveDebug(entity);
            var arch = GetArchetype(entity);
            RemoveComponentInternal(entity, arch, typeId);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool SetComponent(EntityId entity, Type type)
        {
            var meta = GetTypeMetaData(type);
            var compId = meta.TypeId;
            return SetComponent(entity, meta.IsUnmanaged ? new ComponentInfo(compId, meta.UnmanagedSize) : new ComponentInfo(compId, meta.Type));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool SetComponent(EntityId entity, ComponentInfo compInfo)
        {
            ref var compIndexRecord = ref GetEntityIndexRecord(entity);
            var arch = compIndexRecord.Archetype;

            if (!HasComponent(entity, compInfo.TypeId))
            {
                AddComponentInternal(entity, compInfo, arch);
                return true;
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool UnsetComponent(EntityId entity, Type component)
        {
            if (TypeMap.TryGetValue(component, out var meta))
            {
                return UnsetComponent(entity, new ComponentInfo(meta.TypeId, meta.UnmanagedSize));
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool UnsetComponent(EntityId entity, ComponentInfo info)
        {
            ValidateAliveDebug(entity);
            var arch = GetArchetype(entity);
            if (HasComponent(entity, info.TypeId))
            {
                RemoveComponent(entity, info);
                return true;
            }
            return false;
        }

        #endregion

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public bool SetComponent(EntityId entity, object value)
        //{
        //    ValidateAliveDebug(entity);
        //    var type = value.GetType();
        //    ref var compIndexRecord = ref GetEntityIndexRecord(entity);
        //    var arch = compIndexRecord.Archetype;
        //    var meta = GetTypeMetaData(type);
        //    var compId = new int(meta.Id, variant);
        //    var componentInfo = meta.IsUnmanaged ? new ComponentInfo(compId, meta.UnmanagedSize) : new ComponentInfo(compId, meta.Type);
        //    if (HasComponent(entity, componentInfo.int))
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
        public bool SetComponent<T>(EntityId entity) where T : struct, IComponent<T>
        {
            ref var compIndexRecord = ref GetEntityIndexRecord(entity);
            var arch = compIndexRecord.Archetype;
            var compInfo = World.GetOrCreateComponentInfo<T>();

            if (!HasComponent<T>(entity))
            {
                AddComponentInternal(entity, compInfo, arch);
                return true;
            }
            return false;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool SetComponent<T>(EntityId entity, T value) where T : struct, IComponent<T>
        {
            ValidateAliveDebug(entity);
            ref var compIndexRecord = ref GetEntityIndexRecord(entity);
            var arch = compIndexRecord.Archetype;
            var compInfo = World.GetOrCreateComponentInfo<T>();
            if (HasComponent<T>(entity))
            {
                ref T data = ref arch.GetComponent<T>(compIndexRecord.ArchetypeColumn);
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
        public bool UnsetComponent<T>(EntityId entity) where T : struct, IComponent<T>
        {
            ValidateAliveDebug(entity);
            var arch = GetArchetype(entity);
            var compInfo = World.GetOrCreateTypeId<T>();
            if (HasComponent<T>(entity))
            {
                RemoveComponent<T>(entity);
                return true;
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddComponent<T>(EntityId entity) where T : struct, IComponent<T>
        {
            ValidateAliveDebug(entity);
            var arch = GetArchetype(entity);
            var compInfo = World.GetOrCreateComponentInfo<T>();
            ValidateAddDebug(arch, compInfo.TypeId);
            AddComponentInternal(entity, compInfo, arch);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddComponent<T>(EntityId entity, T value) where T : struct, IComponent<T>
        {
            ValidateAliveDebug(entity);
            var arch = GetArchetype(entity);
            var compInfo = World.GetOrCreateComponentInfo<T>();
            ValidateAddDebug(arch, compInfo.TypeId);
            AddComponentWithValueInternal(entity, value, arch);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveComponent<T>(EntityId entity) where T : struct, IComponent<T>
        {
            ValidateAliveDebug(entity);
            var arch = GetArchetype(entity);
            var compInfo = World.GetOrCreateComponentInfo<T>();
            RemoveComponentInternal(entity, arch, compInfo);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasComponent<T>(EntityId entity) where T : struct, IComponent<T>
        {
            int typeId = GetOrCreateTypeId<T>();
            ref EntityIndexRecord record = ref GetEntityIndexRecord(entity);
            Archetype archetype = record.Archetype;
            if (TypeIndexMap.TryGetValue(typeId, out var archetypes))
            {
                return archetypes.ContainsKey(archetype.Index);
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T GetComponent<T>(EntityId entity) where T : struct, IComponent<T>
        {
            ValidateAliveDebug(entity);
            // First check if archetype has id
            ref var record = ref EntityIndex[entity.Id];
            var typeId = GetOrCreateTypeId<T>();
            ValidateHasDebug(record.Archetype, typeId);
            return ref record.Archetype.GetComponent<T>(record.ArchetypeColumn, typeId);
        }

        public void RemoveAllOfType<T>() where T : struct, IComponent<T>
        {
            var all = TypeIndexMap.Where(x => x.Key == GetOrCreateTypeId<T>());
            foreach (var item in all)
            {
                RemoveAllWithComponent<T>();
            }
        }

        public void RemoveAllWithComponent<T>() where T : struct, IComponent<T>
        {
            var archetypes = GetContainingArchetypesWithIndex(GetOrCreateTypeId<T>());
            foreach (var item in archetypes)
            {
                worldArchetypesRWLock.EnterReadLock();
                var arch = AllArchetypes[item.Key];
                worldArchetypesRWLock.ExitReadLock();
                var ents = arch.Entities;
                for (int i = ents.Length - 1; i >= 0; i--)
                {
                    if (arch.IsLocked)
                    {
                        //defList.RemoveOp(ents[i], arch, GetOrCreateArchetypeVariantRemove(arch, new int(GetOrCreateTypeId<T>(), variant)));
                        continue;
                    }
                    RemoveComponent<T>(ents[i]);
                }
            }
        }

        #endregion

        #endregion

        #region Relation Operations
        //private ref SingleRelation GetSingleRelation<T>(EntityId entityId) where T : struct, ISingleRelation<T>
        //{
        //    return ref GetSingleRelationData<T>(entityId, variant).GetRelation();
        //}
        /*     °°°
         *    °°
         *   ^
         *  |°|
         * /   \
         * |[_]|
         * 
         * ^
         * |
         * Forge
         */
        //        private ref TreeRelation GetTreeRelation<T>(EntityId entity) where T : struct, IComponent<T>
        //        {
        //            return ref GetComponent<Rel<T>>(entity).TreeRelation;
        //        }

        //        internal void OnDeleteRelation<T>(ref Rel<T> rel) where T : struct, IComponent<T>
        //        {
        //            var children = rel.TreeRelation.Children;
        //            foreach (var child in children)
        //            {
        //                ClearParent<T>(child);
        //            }
        //        }

        //        //public ref T GetSingleRelationData<T>(EntityId entityId) where T : struct, ISingleRelation<T>
        //        //{
        //        //    ValidateAliveDebug(entityId);
        //        //    // First check if archetype has id
        //        //    ref var record = ref EntityIndex[entityId.Id];
        //        //    var meta = new int(GetOrCreateTypeId<T>(), variant, typeof(T));
        //        //    ValidateHasDebug(record.Archetype, meta);
        //        //    return ref record.Archetype.GetComponent<T>(record.ArchetypeColumn, meta);
        //        //}

        //        public ref T GetTreeRelationData<T>(EntityId entity) where T : struct, IComponent<T>
        //        {
        //            ValidateAliveDebug(entity);
        //            // First check if archetype has id
        //            ref var record = ref EntityIndex[entity.Id];
        //            var typeId = GetOrCreateTypeId<T>();
        //            ValidateHasDebug(record.Archetype, typeId);
        //            return ref record.Archetype.GetComponent<T>(record.ArchetypeColumn, typeId);
        //        }

        //        public bool IsParentOf<T>(EntityId entity, EntityId potentialChild) where T : struct, IComponent<T>
        //        {
        //            ref var relation = ref GetTreeRelation<T>(potentialChild);
        //            return relation.parentInternal == entity;
        //        }

        //        public bool IsChildOf<T>(EntityId entity, EntityId potentialParent) where T : struct, IComponent<T>
        //        {
        //            ref var relation = ref GetTreeRelation<T>(entity);
        //            return relation.parentInternal == potentialParent;
        //        }

        //        public bool IsDecendantOf<T>(EntityId entity, EntityId potentialDecendant) where T : struct, IComponent<T>
        //        {
        //            return IsAncestorOf<T>(potentialDecendant, entity);
        //        }

        //        public bool IsAncestorOf<T>(EntityId entity, EntityId potentialAncestor) where T : struct, IComponent<T>
        //        {
        //            ref var relation = ref GetTreeRelation<T>(entity);

        //            if (relation.parentInternal == potentialAncestor)
        //            {
        //                return true;
        //            }

        //            if (!relation.parentInternal.HasValue)
        //            {
        //                return true;
        //            }

        //            return IsAncestorOf<T>(relation.parentInternal.Value, potentialAncestor);
        //        }

        //        public void SetParent<T>(EntityId entity, EntityId parent) where T : struct, IComponent<T>
        //        {
        //#if DEBUG
        //            if (entity == parent)
        //            {
        //                ThrowHelper.ThrowArgumentException("Tried to set itself as item parentInternal entity");
        //            }

        //            if (IsDecendantOf<T>(entity, parent))
        //            {
        //                ThrowHelper.ThrowArgumentException("Tried to set item decendant as the parentInternal of this entity");
        //            }
        //#endif
        //            ref var relation = ref GetTreeRelation<T>(entity);
        //            if (relation.parentInternal.HasValue)
        //            {
        //                ref var relation2 = ref GetTreeRelation<T>(relation.parentInternal.Value);
        //                relation2.RemoveChild(entity);
        //            }
        //            relation.parentInternal = parent;
        //            ref var relation3 = ref GetTreeRelation<T>(parent);
        //            relation3.AddChild(entity);
        //        }

        //        public void AddChild<T>(EntityId entity, EntityId child) where T : struct, IComponent<T>
        //        {
        //#if DEBUG
        //            if (entity == child)
        //            {
        //                ThrowHelper.ThrowArgumentException("Tried to add itself as item child entity");
        //            }

        //            if (IsAncestorOf<T>(entity, child))
        //            {
        //                ThrowHelper.ThrowArgumentException("Tried to add item child that is already an ancestor of this relation");
        //            }
        //#endif
        //            ref var relation = ref GetTreeRelation<T>(entity);
        //            relation.AddChild(child);
        //            ref var relation2 = ref GetTreeRelation<T>(child);
        //            if (relation2.parentInternal.HasValue)
        //            {
        //                ref var relation3 = ref GetTreeRelation<T>(relation2.parentInternal.Value);
        //                relation3.RemoveChild(child);
        //            }
        //            relation2.parentInternal = entity;
        //        }

        //        public void RemoveChild<T>(EntityId entity, EntityId child) where T : struct, IComponent<T>
        //        {
        //            ref var relation = ref GetTreeRelation<T>(entity);
        //            relation.RemoveChild(child);
        //            ref var relation2 = ref GetTreeRelation<T>(child);
        //            relation2.parentInternal = null;
        //        }

        //        public void ClearParent<T>(EntityId entity) where T : struct, IComponent<T>
        //        {
        //            ref var relation = ref GetTreeRelation<T>(entity);
        //            if (relation.parentInternal.HasValue)
        //            {
        //                ref var relation2 = ref GetTreeRelation<T>(relation.parentInternal.Value);
        //                relation2.RemoveChild(entity);
        //                relation.parentInternal = null;
        //            }
        //        }

        //        public ReadOnlySpan<EntityId> GetChildren<T>(EntityId entity) where T : struct, IComponent<T>
        //        {
        //            return GetTreeRelation<T>(entity).Children;
        //        }

        //        public EntityId? GetParent<T>(EntityId entity) where T : struct, IComponent<T>
        //        {
        //            return GetTreeRelation<T>(entity).parentInternal;
        //        }

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
                var arch = AllArchetypes[index];
                worldArchetypesRWLock.ExitReadLock();
                return arch;
            }
            worldArchetypesRWLock.ExitReadLock();
            return null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Archetype GetOrCreateArchetypeVariantAdd(Archetype source, ComponentInfo compInfo)
        {
            Archetype? archetype;
            //Archetype already stored in graph
            if (source.TryGetSiblingAdd(compInfo.TypeId, out archetype))
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
                source.SetSiblingAdd(compInfo.TypeId, archetype);
                return archetype;
            }
            //Archetype does not yet exist, create it!
            var definition = new ArchetypeDefinition(hash, memory.ToArray());
            archetype = CreateArchetype(definition);
            ArrayPool<ComponentInfo>.Shared.Return(pool);
            source.SetSiblingAdd(compInfo.TypeId, archetype);
            return archetype;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Archetype GetOrCreateArchetypeVariantRemove(Archetype source, int compInfo)
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
                if (compPool.TypeId != compInfo)
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
                int id = infos[i].TypeId;
                mask.SetBit(id);
            }
            var archetype = new Archetype(this, definition.ComponentInfos, mask, definition.HashCode, archetypeCount);
            worldArchetypesRWLock.EnterWriteLock();
            // Store in index
            for (int i = 0; i < infos.Length; i++)
            {
                ref var dict = ref CollectionsMarshal.GetValueRefOrAddDefault(TypeIndexMap, infos[i].TypeId, out var exists);
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
            worldFilterRWLock.ExitUpgradeableReadLock();
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
            worldArchetypesRWLock.EnterWriteLock();
            worldFilterRWLock.EnterWriteLock();
            worldEntitiesRWLock.EnterWriteLock();
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
            worldEntitiesRWLock.ExitWriteLock();
            worldFilterRWLock.ExitWriteLock();
            worldArchetypesRWLock.ExitWriteLock();
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

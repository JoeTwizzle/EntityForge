using EntityForge.Collections;
using EntityForge.Collections.Generic;
using EntityForge.Core;
using EntityForge.Helpers;
using EntityForge.Queries;
using EntityForge.Tags;
using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace EntityForge
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
        internal static readonly ArchetypeDefinition EmptyArchetypeDefinition = new ArchetypeDefinition(GetComponentHash(Array.Empty<ComponentInfo>()), Array.Empty<ComponentInfo>());
        private static readonly List<short> recycledWorlds = new();
        private static readonly ReaderWriterLockSlim createTypeRWLock = new();
        private static readonly object createWorldLock = new object();
        private static World[] worlds = new World[1];
        private static short worldCounter;
        private static int componentCount;
        private static int flagCount;

        /// <summary>
        /// Stores the meta item id has
        /// </summary>
        private static readonly Dictionary<Type, ComponentInfo> TypeMap = new();
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
        /// componentID, archetype.Index -> archetype.ComponentTypeIndex
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
        internal ArchetypeFilter[] AllFilters;
        /// <summary>
        /// The meta of this World
        /// </summary>
        public short WorldId { get; private init; }
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
        public ReadOnlySpan<ArchetypeFilter> Filters => new ReadOnlySpan<ArchetypeFilter>(AllFilters, 0, filterCount);

        int filterCount;
        int archetypeCount;
        int entityCounter;
        int recycledEntitiesCount;

        public World()
        {
            lock (createWorldLock)
            {
                AllArchetypes = new Archetype[DefaultComponents];
                AllFilters = new ArchetypeFilter[DefaultComponents];
                FilterMap = new(DefaultComponents);
                ArchetypeIndexMap = new(DefaultComponents);
                TypeIndexMap = new(DefaultComponents);
                EntityIndex = new EntityIndexRecord[DefaultEntities];
                RecycledEntities = new EntityId[4];

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

        private static short GetNextWorldId()
        {
            if (recycledWorlds.Count > 0)
            {
                int lastIdx = recycledWorlds.Count - 1;
                short id = recycledWorlds[lastIdx];
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
        public static ComponentInfo GetComponentInfo(Type type)
        {
            createTypeRWLock.EnterReadLock();
            var meta = TypeMap[type];
            createTypeRWLock.ExitReadLock();
            return meta;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ComponentInfo GetComponentInfo(int typeId)
        {
            createTypeRWLock.EnterReadLock();
            var meta = TypeMap[TypeMapReverse[typeId]];
            createTypeRWLock.ExitReadLock();
            return meta;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetOrCreateTagId<T>() where T : struct, ITag<T>
        {
            if (T.BitIndex == 0)
            {
                T.BitIndex = Interlocked.Increment(ref flagCount);
            }
            return T.BitIndex;
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

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void CreateTypeId<T>() where T : struct, IComponent<T>
        {
            createTypeRWLock.EnterWriteLock();
            ref var info = ref CollectionsMarshal.GetValueRefOrAddDefault(TypeMap, typeof(T), out var exists);
            if (!exists)
            {
                int size = RuntimeHelpers.IsReferenceOrContainsReferences<T>() ? 0 : Unsafe.SizeOf<T>();
                int id = ++componentCount;
                TypeMapReverse.Add(id, typeof(T));
                info = new ComponentInfo(id, size, typeof(T));
                T.Id = id;
                T.Registered = true;
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
                return new ComponentInfo(GetOrCreateTypeId<T>(), Unsafe.SizeOf<T>(), typeof(T));
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
                    hash = hash * 486187739 + componentTypes[i].TypeId;
                }
                return hash;
            }
        }

        #endregion

        #region Helpers

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsAlive(Entity entity)
        {
            return entity.EntityId.Id < entityCounter && EntityIndex[entity.EntityId.Id].EntityVersion == entity.Version;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsAlive(EntityId entity)
        {
            return entity.Id < entityCounter && EntityIndex[entity.Id].EntityVersion > 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Archetype GetArchetype(EntityId entity)
        {
            return GetEntityIndexRecord(entity).Archetype;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Archetype GetArchetypeById(int id)
        {
            worldArchetypesRWLock.EnterReadLock();
            var arch = AllArchetypes[id];
            worldArchetypesRWLock.ExitReadLock();
            return arch;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref EntityIndexRecord GetEntityIndexRecord(EntityId entity)
        {
            return ref EntityIndex[entity.Id];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Dictionary<int, TypeIndexRecord> GetContainingArchetypesWithType<T>() where T : struct, IComponent<T>
        {
            return TypeIndexMap[GetOrCreateTypeId<T>()];
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
                ThrowHelper.ThrowMissingComponentException($"The entity does not have a Component with typeId {type}");
            }
        }

        [Conditional("DEBUG")]
        private static void ValidateAddDebug(Archetype archetype, int type)
        {
            if (archetype.HasComponent(type))
            {
                ThrowHelper.ThrowDuplicateComponentException($"Tried adding duplicate Component with typeId {type}");
            }
        }

        [Conditional("DEBUG")]
        private static void ValidateRemoveDebug(Archetype archetype, int type)
        {
            if (!archetype.HasComponent(type))
            {
                ThrowHelper.ThrowMissingComponentException($"Tried removing missing Component with typeId {type}");
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

        public void ReserveEntities(in ArchetypeDefinition definition, int count)
        {
            var archetype = GetOrCreateArchetype(definition);
            if (archetype.IsLocked)
            {
                archetype.CommandBuffer.Reserve(count);
                return;
            }
            archetype.GrowBy(count);
        }

        public Entity CreateEntity()
        {
            return CreateEntity(EmptyArchetypeDefinition);
        }

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
            ref var entIndex = ref EntityIndex[entityId.Id];
            entIndex.Archetype = archetype;
            entIndex.EntityVersion = (short)-entIndex.EntityVersion;
            entIndex.ArchetypeColumn = archetype.ElementCount;
            var entity = new Entity(entityId.Id, entIndex.EntityVersion, WorldId);
            if (archetype.IsLocked)
            {
                entIndex.ArchetypeColumn = archetype.CommandBuffer.Create(entityId);
                return entity;
            }
            archetype.AddEntityInternal(entity);
            InvokeCreateEntityEvent(entityId);
            return entity;
        }

        /// <summary>
        /// Create <paramref name="count"/> entities in the default ArchetypeDefinition.
        /// </summary>
        /// <param name="count">Number of entities to create.</param>
        /// <remarks>Note: Does not recycle entites only create new ones, use sparingly.</remarks>
        /// <returns>EntityCollection of the created Entities.</returns>
        public EntityRange CreateEntities(int count)
        {
            return CreateEntities(EmptyArchetypeDefinition, count);
        }

        /// <summary>
        /// Create <paramref name="count"/> entities in the speciefied Archetype.
        /// </summary>
        /// <param name="definition">The archetype to create entities in.</param>
        /// <param name="count">Number of entities to create.</param>
        /// <remarks>Note: Does not recycle entites only create new ones, use sparingly.</remarks>
        /// <returns>EntityCollection of the created Entities.</returns>
        public EntityRange CreateEntities(in ArchetypeDefinition definition, int count)
        {
            if (count <= 0)
            {
                throw new ArgumentException($"{nameof(count)} must be greater than 0.");
            }
            var archetype = GetOrCreateArchetype(definition);
            worldEntitiesRWLock.EnterWriteLock();
            EntityIndex = EntityIndex.GrowIfNeeded(entityCounter, count);
            var entityIndices = EntityIndex.AsSpan(entityCounter, count);
            var ents = new EntityRange(WorldId, entityCounter, count);
            if (archetype.IsLocked)
            {
                var start = archetype.CommandBuffer.CreateMany(entityCounter, count);
                for (int i = 0; i < count; i++)
                {
                    ref var entIndex = ref entityIndices[i];
                    entIndex.Archetype = archetype;
                    entIndex.EntityVersion = 1;
                    entIndex.ArchetypeColumn = start + i;
                }
                entityCounter += count;
                worldEntitiesRWLock.ExitWriteLock();
                return ents;
            }
            archetype.GrowBy(count);
            var entities = MemoryMarshal.CreateSpan(ref archetype.EntitiesPool.GetRefAt(archetype.EntityCount), count);
            for (int i = 0; i < count; i++)
            {
                ref var entIndex = ref entityIndices[i];
                entIndex.Archetype = archetype;
                entIndex.EntityVersion = 1;
                entIndex.ArchetypeColumn = archetype.ElementCount + i;

                entities[i] = new Entity(entityCounter + i, entIndex.EntityVersion, WorldId);
            }
            archetype.ElementCount += count;
            entityCounter += count;
            worldEntitiesRWLock.ExitWriteLock();
            return ents;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void InvokeCreateEntityEvent(EntityId entityId)
        {
            if (EntityEventsEnabled)
            {
                OnEntityCreated?.Invoke(entityId);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void InvokeDeleteEntityEvent(EntityId entityId)
        {
            if (EntityEventsEnabled)
            {
                OnEntityDelete?.Invoke(entityId);
            }
        }


        public void DeleteEntity(EntityId entityId)
        {
            ValidateAliveDebug(entityId);

            ref EntityIndexRecord compIndexRecord = ref GetEntityIndexRecord(entityId);
            var src = compIndexRecord.Archetype;
            //Get index of entityId to be removed
            ref var entityIndex = ref EntityIndex[entityId.Id];
            //Set its version to its negative increment (Mark entityId as destroyed)
            entityIndex.EntityVersion = (short)-(entityIndex.EntityVersion + 1);
            worldEntitiesRWLock.EnterWriteLock();
            RecycledEntities = RecycledEntities.GrowIfNeeded(recycledEntitiesCount, 1);
            RecycledEntities[recycledEntitiesCount++] = entityId;
            worldEntitiesRWLock.ExitWriteLock();
            if (src.IsLocked)
            {
                src.CommandBuffer.Destroy(entityId);
                return;
            }

            DeleteEntityInternal(src, compIndexRecord.ArchetypeColumn);
            InvokeDeleteEntityEvent(entityId);
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
            return new Entity(id.Id, GetEntityIndexRecord(id).EntityVersion, WorldId);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal int MoveEntity(Archetype src, Archetype dest, EntityId entity)
        {
            Debug.Assert(src.Index != dest.Index && !src.IsLocked);

            ref EntityIndexRecord compIndexRecord = ref GetEntityIndexRecord(entity);
            int oldIndex = compIndexRecord.ArchetypeColumn;
            dest.GrowBy(1);
            dest.EntitiesPool.GetRefAt(dest.ElementCount) = new Entity(entity.Id, compIndexRecord.EntityVersion, WorldId);
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
                arch.CommandBuffer.Add(entity, info);
            }
            else
            {
                ValidateAddDebug(arch, info.TypeId);
                var newArch = GetOrCreateArchetypeVariantAdd(arch, info);
                MoveEntity(arch, newArch, entity);
                InvokeComponentAddEvent(entity, info.TypeId);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void AddComponentWithValueInternal<T>(EntityId entity, T value, Archetype arch) where T : struct, IComponent<T>
        {
            var info = GetOrCreateComponentInfo<T>();
            if (arch.IsLocked)
            {
                arch.CommandBuffer.AddWithValue(entity, value);
            }
            else
            {
                var newArch = GetOrCreateArchetypeVariantAdd(arch, info);
                var index = MoveEntity(arch, newArch, entity);
                var typeIndexRecord = GetTypeIndexRecord(newArch, info.TypeId).ComponentTypeIndex;
                ref T data = ref newArch.GetComponentByIndex<T>(index, typeIndexRecord);
                data = value;
                InvokeComponentAddEvent(entity, info.TypeId);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void RemoveComponentInternal(EntityId entity, Archetype arch, ComponentInfo info)
        {
            if (arch.IsLocked)
            {
                arch.CommandBuffer.Remove(entity, info);
            }
            else
            {
                ValidateRemoveDebug(arch, info.TypeId);
                var newArch = GetOrCreateArchetypeVariantRemove(arch, info.TypeId);
                InvokeComponentRemoveEvent(entity, info.TypeId);
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
                        //copy from components to archetypes components
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
            AddComponent(entity, GetComponentInfo(type));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddComponent(EntityId entity, ComponentInfo compInfo)
        {
            ValidateAliveDebug(entity);
            var arch = GetArchetype(entity);
            AddComponentInternal(entity, compInfo, arch);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveComponent(EntityId entity, Type type)
        {
            if (TypeMap.TryGetValue(type, out var meta))
            {
                RemoveComponent(entity, GetComponentInfo(type));
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
            return SetComponent(entity, GetComponentInfo(type));
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
                return UnsetComponent(entity, GetComponentInfo(component));
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

        #region Generic

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
            var typeId = World.GetOrCreateTypeId<T>();
            ref EntityIndexRecord record = ref GetEntityIndexRecord(entity);
            if (record.Archetype.TryGetComponentIndex<T>(out int index))
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
            ref var record = ref GetEntityIndexRecord(entity);
            var compInfo = World.GetOrCreateComponentInfo<T>();
            AddComponentInternal(entity, compInfo, record.Archetype);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddComponent<T>(EntityId entity, T value) where T : struct, IComponent<T>
        {
            ValidateAliveDebug(entity);
            var arch = GetArchetype(entity);
            var compInfo = World.GetOrCreateComponentInfo<T>();
            AddComponentWithValueInternal(entity, value, arch);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveComponent<T>(EntityId entity) where T : struct, IComponent<T>
        {
            ValidateAliveDebug(entity);
            ref var record = ref GetEntityIndexRecord(entity);
            var compInfo = World.GetOrCreateComponentInfo<T>();
            RemoveComponentInternal(entity, record.Archetype, compInfo);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasComponent<T>(EntityId entity) where T : struct, IComponent<T>
        {
            ValidateAliveDebug(entity);
            ref EntityIndexRecord record = ref GetEntityIndexRecord(entity);
            if (record.Archetype.IsLocked)
            {
                return record.Archetype.CommandBuffer.HasComponent(entity, GetOrCreateTypeId<T>());
            }
            return record.Archetype.HasComponent(GetOrCreateTypeId<T>());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T GetComponent<T>(EntityId entity) where T : struct, IComponent<T>
        {
            ValidateAliveDebug(entity);
            // First check if archetype has id
            ref EntityIndexRecord record = ref GetEntityIndexRecord(entity);
            var typeId = GetOrCreateTypeId<T>();
            if (record.Archetype.IsLocked)
            {
                return ref record.Archetype.CommandBuffer.GetComponent<T>(entity);
            }
            ValidateHasDebug(record.Archetype, typeId);
            return ref record.Archetype.GetComponent<T>(record.ArchetypeColumn, typeId);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T GetComponentOrNullRef<T>(EntityId entity) where T : struct, IComponent<T>
        {
            ValidateAliveDebug(entity);
            // First check if archetype has id
            ref EntityIndexRecord record = ref GetEntityIndexRecord(entity);
            int typeId = GetOrCreateTypeId<T>();
            Archetype archetype = record.Archetype;
            if (record.Archetype.HasComponent(typeId))
            {
                return ref record.Archetype.GetComponent<T>(record.ArchetypeColumn, typeId);
            }
            if (record.Archetype.IsLocked)
            {
                return ref record.Archetype.CommandBuffer.GetComponentOrNullRef<T>(entity);
            }
            return ref Unsafe.NullRef<T>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T SetComponent<T>(EntityId entity) where T : struct, IComponent<T>
        {
            ref var record = ref GetEntityIndexRecord(entity);
            var compInfo = World.GetOrCreateComponentInfo<T>();
            if (!record.Archetype.HasComponent(compInfo.TypeId))
            {
                AddComponentInternal(entity, compInfo, record.Archetype);
            }
            return ref record.Archetype.GetComponent<T>(record.ArchetypeColumn, compInfo.TypeId);
        }

        public void RemoveComponentFromAll<T>() where T : struct, IComponent<T>
        {
            var archetypes = GetContainingArchetypesWithType<T>();
            foreach (var item in archetypes)
            {
                var arch = GetArchetypeById(item.Key);
                var ents = arch.Entities;
                for (int i = ents.Length - 1; i >= 0; i--)
                {
                    RemoveComponent<T>(ents[i]);
                }
            }
        }

        #endregion

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
        internal Archetype GetOrCreateArchetypeVariantAdd(Archetype source, ComponentInfo compInfo)
        {
            //Archetype already stored in graph
            if (source.TryGetSiblingAdd(compInfo.TypeId, out Archetype? archetype))
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Archetype GetOrCreateArchetypeVariantRemove(Archetype source, int compInfo)
        {
            //Archetype already stored in graph
            if (source.TryGetSiblingRemove(compInfo, out Archetype? archetype))
            {
                return archetype;
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
        public ArchetypeFilter GetArchetypeFilter(ComponentMask mask)
        {
            worldFilterRWLock.EnterUpgradeableReadLock();
            ref var filterId = ref CollectionsMarshal.GetValueRefOrAddDefault(FilterMap, mask, out bool exists);
            ArchetypeFilter filter;
            if (exists)
            {
                filter = AllFilters[filterId];
                worldFilterRWLock.ExitUpgradeableReadLock();
                return filter;
            }
            filter = new ArchetypeFilter(this, mask);
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
            componentCount = 0;
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

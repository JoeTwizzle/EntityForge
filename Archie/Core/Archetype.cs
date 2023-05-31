using Archie.Collections;
using Archie.Collections.Generic;
using Archie.Commands;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Archie
{
    public unsafe sealed class Archetype : IDisposable
    {
        public const int DefaultPoolSize = 8;
        /// <summary>
        /// Unique Index of this Archetype
        /// </summary>
        public readonly int Index;
        /// <summary>
        /// Hash based on which components this archertype has
        /// </summary>
        public readonly int Hash;
        /// <summary>
        /// BitMask of which components this archtype contains
        /// </summary>
        internal readonly BitMask ComponentMask;
        /// <summary>
        /// Connections to Archetypes differing by only one typeId
        /// </summary>
        internal readonly Dictionary<int, ArchetypeSiblings> Siblings;
        /// <summary>
        /// Maps at which index components of a given typeid are stored
        /// </summary>
        internal readonly Dictionary<int, int> ComponentIdsMap;
        internal readonly ArchetypeCommandBuffer CommandBuffer;
        internal readonly ReadOnlyMemory<ComponentInfo> ComponentInfo;
        internal readonly ArrayOrPointer[] ComponentPools;
        internal readonly ArrayOrPointer<Entity> EntitiesPool;

        internal int PropertyCount => ComponentPools.Length;
        internal int ElementCapacity;
        internal int ElementCount;


        /// <summary>
        /// BitMask signaling which components are accessed as writable
        /// </summary>
        private readonly BitMask writeMask;
        /// <summary>
        /// BitMask signaling which components are being accessed
        /// </summary>
        private readonly BitMask accessMask;
        private readonly ReaderWriterLockSlim poolAccessLock;
        private readonly ReaderWriterLockSlim siblingAccessLock;
        private int lockCount;

        public bool IsLocked
        {
            get
            {
                return lockCount != 0;
            }
        }

        public int EntityCount
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return ElementCount;
            }
        }

        public Span<Entity> Entities
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return EntitiesPool.GetSpan(ElementCount);
            }
        }

        public World World { get; }

        public Archetype(World world, ReadOnlyMemory<ComponentInfo> componentInfo, BitMask bitMask, int hash, int index)
        {
            World = world;
            CommandBuffer = new();
            writeMask = new();
            poolAccessLock = new();
            siblingAccessLock = new();
            accessMask = new();
            ComponentMask = bitMask;
            Hash = hash;
            Index = index;
            Siblings = new();
            ComponentIdsMap = new();
            ComponentInfo = componentInfo;
            ComponentPools = new ArrayOrPointer[componentInfo.Length];
            var infos = ComponentInfo.Span;
            for (int i = 0; i < componentInfo.Length; i++)
            {
                ref readonly var compInfo = ref infos[i];
                ComponentIdsMap.Add(compInfo.TypeId, i);
                ComponentPools[i] = ArrayOrPointer.CreateForComponent(compInfo, Archetype.DefaultPoolSize);
            }
            EntitiesPool = ArrayOrPointer<Entity>.Create(Archetype.DefaultPoolSize);
            ElementCapacity = Archetype.DefaultPoolSize;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void AddEntityInternal(Entity entity)
        {
            GrowBy(1);
            EntitiesPool.GetRefAt(ElementCount++) = entity;
        }

        public void GrowBy(int added)
        {

            int desiredSize = ElementCount + added;
            if (desiredSize >= ElementCapacity)
            {
                poolAccessLock.EnterWriteLock();
                int newCapacity = (int)BitOperations.RoundUpToPowerOf2((uint)desiredSize + 1);
                var infos = ComponentInfo.Span;
                for (int i = 0; i < ComponentPools.Length; i++)
                {
                    ref var pool = ref ComponentPools[i];
                    if (pool.IsUnmanaged)
                    {
                        pool.GrowToUnmanaged(newCapacity, infos[i].UnmanagedSize);
                    }
                    else
                    {
                        pool.GrowToManaged(newCapacity, infos[i].Type!);
                    }
                }
                EntitiesPool.GrowTo(newCapacity);
                ElementCapacity = newCapacity;
                poolAccessLock.ExitWriteLock();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void FillHole(int holeIndex)
        {
            poolAccessLock.EnterWriteLock();
            var infos = ComponentInfo.Span;
            //Swap last item with the removed item
            for (int i = 0; i < ComponentPools.Length; i++)
            {
                var pool = ComponentPools[i];
                if (pool.IsUnmanaged)
                {
                    pool.FillHoleUnmanaged(holeIndex, ElementCount - 1, infos[i].UnmanagedSize);
                }
                else
                {
                    pool.FillHoleManaged(holeIndex, ElementCount - 1);
                }
            }
            poolAccessLock.ExitWriteLock();
        }

        public Span<T> GetPool<T>() where T : struct, IComponent<T>
        {
            ref var pool = ref ComponentPools[GetComponentIndex(World.GetOrCreateTypeId<T>())];
            if (pool.IsUnmanaged)
            {
                return new Span<T>(pool.UnmanagedData, ElementCount);
            }
            else
            {
                return new Span<T>((T[])pool.ManagedData!, 0, ElementCount);
            }
        }

        internal ref T GetRef<T>(int index) where T : struct, IComponent<T>
        {
            ref var pool = ref ComponentPools[GetComponentIndex(World.GetOrCreateTypeId<T>())];
            if (pool.IsUnmanaged)
            {
                return ref ((T*)pool.UnmanagedData)[index];
            }
            else
            {
                return ref ((T[])pool.ManagedData!)[index];
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void CopyComponents(int srcIndex, Archetype dest, int destIndex)
        {
            poolAccessLock.EnterReadLock();
            dest.poolAccessLock.EnterWriteLock();
            var infos = dest.ComponentInfo.Span;
            for (int i = 0; i < dest.ComponentInfo.Length; i++)
            {
                ref readonly var compInfo = ref infos[i];
                if (ComponentIdsMap.TryGetValue(compInfo.TypeId, out var index))
                {
                    if (compInfo.IsUnmanaged)
                    {
                        ComponentPools[index].CopyToUnmanaged(srcIndex, dest.ComponentPools[i].UnmanagedData, destIndex, compInfo.UnmanagedSize);
                    }
                    else
                    {
                        ComponentPools[index].CopyToManaged(srcIndex, dest.ComponentPools[i].ManagedData!, destIndex, 1);
                    }
                }
            }
            dest.poolAccessLock.ExitWriteLock();
            poolAccessLock.ExitReadLock();
        }

        #region Siblings

        public ArchetypeSiblings SetSiblingAdd(int typeId, Archetype sibling)
        {
            siblingAccessLock.EnterWriteLock();
            if (!Siblings.TryGetValue(typeId, out var archetypes))
            {
                archetypes = new ArchetypeSiblings(sibling, null);
                Siblings.Add(typeId, archetypes);
                siblingAccessLock.ExitWriteLock();
                return archetypes;
            }
            archetypes.Add = sibling;
            Siblings[typeId] = archetypes;
            siblingAccessLock.ExitWriteLock();
            return archetypes;
        }

        public ArchetypeSiblings SetSiblingRemove(int typeId, Archetype sibling)
        {
            siblingAccessLock.EnterWriteLock();
            if (!Siblings.TryGetValue(typeId, out var archetypes))
            {
                archetypes = new ArchetypeSiblings(null, sibling);
                Siblings.Add(typeId, archetypes);
                siblingAccessLock.ExitWriteLock();
                return archetypes;
            }
            archetypes.Remove = sibling;
            Siblings[typeId] = archetypes;
            siblingAccessLock.ExitWriteLock();
            return archetypes;
        }

        public bool HasSiblingAdd(int typeId)
        {
            siblingAccessLock.EnterReadLock();
            if (!Siblings.TryGetValue(typeId, out var archetypes))
            {
                siblingAccessLock.ExitReadLock();
                return false;
            }
            var value = archetypes.Add != null;
            siblingAccessLock.ExitReadLock();
            return value;
        }

        public bool HasSiblingRemove(int typeId)
        {
            siblingAccessLock.EnterReadLock();
            if (!Siblings.TryGetValue(typeId, out var archetypes))
            {
                siblingAccessLock.ExitReadLock();
                return false;
            }
            var value = archetypes.Remove != null;
            siblingAccessLock.ExitReadLock();
            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetSiblingAdd(int typeId, [NotNullWhen(true)] out Archetype? siblingAdd)
        {
            siblingAccessLock.EnterReadLock();
            if (!Siblings.TryGetValue(typeId, out var archetypes))
            {
                siblingAccessLock.ExitReadLock();
                siblingAdd = null;
                return false;
            }
            siblingAdd = archetypes.Add;
            siblingAccessLock.ExitReadLock();
            return siblingAdd != null;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetSiblingRemove(int typeId, [NotNullWhen(true)] out Archetype? siblingRemove)
        {
            siblingAccessLock.EnterReadLock();
            if (!Siblings.TryGetValue(typeId, out var archetypes))
            {
                siblingAccessLock.ExitReadLock();
                siblingRemove = null;
                return false;
            }
            siblingRemove = archetypes.Remove;
            siblingAccessLock.ExitReadLock();
            return siblingRemove != null;
        }

        #endregion

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetComponentIndex(int compInfo)
        {
            return ComponentIdsMap[compInfo];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T GetComponentByIndex<T>(int entityIndex, int compIndex) where T : struct, IComponent<T>
        {
            return ref (ComponentPools[compIndex].GetRefAt<T>(entityIndex));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T GetComponent<T>(int entityIndex, int compInfo) where T : struct, IComponent<T>
        {
            return ref (ComponentPools[GetComponentIndex(compInfo)].GetRefAt<T>(entityIndex));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T GetComponent<T>(int entityIndex) where T : struct, IComponent<T>
        {
            return ref GetComponent<T>(entityIndex, World.GetOrCreateTypeId<T>());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetComponent(int entityIndex, ComponentInfo info, object data)
        {
            var pool = ComponentPools[GetComponentIndex(info.TypeId)];
            if (pool.IsUnmanaged)
            {
                var destAddress = (((byte*)pool.UnmanagedData) + entityIndex * info.UnmanagedSize);
                Unsafe.CopyBlock(ref Unsafe.AsRef<byte>(destAddress), ref Unsafe.Unbox<byte>(data), (uint)info.UnmanagedSize);
            }
            else
            {
                pool.ManagedData.SetValue(data, entityIndex);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasComponent<T>() where T : struct, IComponent<T>
        {
            return HasComponent(World.GetOrCreateTypeId<T>());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasComponent(int id)
        {
            return ComponentIdsMap.ContainsKey(id);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void GetAccess(ComponentMask mask)
        {
            poolAccessLock.EnterWriteLock();
            accessMask.OrBits(mask.HasMask);
            for (int j = 0; j < mask.SomeMasks.Length; j++)
            {
                accessMask.OrBits(mask.SomeMasks[j]);
            }
            poolAccessLock.ExitWriteLock();
            poolAccessLock.EnterReadLock();
            bool isConflict = writeMask.AnyMatch(mask.WriteMask);
            poolAccessLock.ExitReadLock();
            while (isConflict)
            {
                poolAccessLock.EnterReadLock();
                isConflict = writeMask.AnyMatch(mask.WriteMask);
                poolAccessLock.ExitReadLock();
            }
            poolAccessLock.EnterWriteLock();
            //Set the components we write to
            writeMask.OrFilteredBits(mask.WriteMask, ComponentMask);
            poolAccessLock.ExitWriteLock();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ReleaseAccess(ComponentMask mask)
        {
            poolAccessLock.EnterWriteLock();
            writeMask.ClearFilteredBits(mask.WriteMask, ComponentMask);
            accessMask.ClearBits(mask.HasMask);
            for (int j = 0; j < mask.SomeMasks.Length; j++)
            {
                accessMask.ClearBits(mask.SomeMasks[j]);
            }
            poolAccessLock.ExitWriteLock();
        }

        public void Dispose()
        {
            CommandBuffer.Dispose();
            poolAccessLock.Dispose();
            siblingAccessLock.Dispose();
            for (int i = 0; i < ComponentPools.Length; i++)
            {
                ComponentPools[i].Dispose();
            }
            EntitiesPool.Dispose();
        }


        public void Lock()
        {
            Interlocked.Increment(ref lockCount);
        }

        public void Unlock()
        {
            var val = Interlocked.Decrement(ref lockCount);
            if (val == 0)
            {
                CommandBuffer.Execute(World, this);
            }
        }

        internal static bool Contains(Archetype archetype, int type)
        {
            return archetype.ComponentIdsMap.ContainsKey(type);
        }
    }
}

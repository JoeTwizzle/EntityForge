using EntityForge.Collections;
using EntityForge.Collections.Generic;
using EntityForge.Commands;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace EntityForge
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
        internal readonly UnsafeSparseSet<int> ComponentIdsMap;
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
        private readonly int[] readAccessMask;
        private readonly ReaderWriterLockSlim poolAccessLock;
        private readonly ReaderWriterLockSlim siblingAccessLock;
        private int lockCount;

        /// <summary>
        /// True when Archetype is currently being iterated and will defer all changes until iteration finishes.
        /// </summary>
        public bool IsLocked
        {
            get
            {
                return lockCount != 0;
            }
        }

        /// <summary>
        /// Number of entities currently in this Archetype
        /// </summary>
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
            readAccessMask = new int[componentInfo.Length];
            poolAccessLock = new();
            siblingAccessLock = new();
            ComponentMask = bitMask;
            Hash = hash;
            Index = index;
            Siblings = new();
            ComponentIdsMap = new();
            ComponentInfo = componentInfo;
            ComponentPools = new ArrayOrPointer[componentInfo.Length];
            ElementCapacity = Archetype.DefaultPoolSize;
            var infos = ComponentInfo.Span;
            for (int i = 0; i < componentInfo.Length; i++)
            {
                ref readonly var compInfo = ref infos[i];
                ComponentIdsMap.Add(compInfo.TypeId, i);
                ComponentPools[i] = ArrayOrPointer.CreateForComponent(compInfo, ElementCapacity);
                if (compInfo.IsUnmanaged)
                {
                    MemoryMarshal.CreateSpan(ref ComponentPools[i].GetFirst<byte>(), ElementCapacity * infos[i].UnmanagedSize).Clear();
                }
            }

            EntitiesPool = ArrayOrPointer<Entity>.Create(ElementCapacity);
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
                        MemoryMarshal.CreateSpan(ref pool.GetRefAt<byte>(ElementCapacity * infos[i].UnmanagedSize), (newCapacity - ElementCapacity) * infos[i].UnmanagedSize).Clear();
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
        public int GetComponentIndex(int typeId)
        {
            return ComponentIdsMap.GetValue(typeId);
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
        public bool TryGetComponentIndex<T>(out int index) where T : struct, IComponent<T>
        {
            return ComponentIdsMap.TryGetValue(World.GetOrCreateTypeId<T>(), out index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasComponent(int id)
        {
            return ComponentIdsMap.Has(id);
        }

        /// <summary>
        /// Blocks adding or removing components & entities until a later time
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Lock()
        {
            Interlocked.Increment(ref lockCount);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Unlock()
        {
            var val = Interlocked.Decrement(ref lockCount);
            if (val == 0 && CommandBuffer.cmdCount > 0)
            {
                CommandBuffer.Execute(World, this);
            }
        }
        /// <summary>
        /// Tries to get access to the pools defined by this mask
        /// </summary>
        /// <param name="mask"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void GetAccess(ComponentMask mask)
        {
            bool hasConflict = false;
            do
            {
                poolAccessLock.EnterUpgradeableReadLock();
                //Check if any of these pools are already being written to
                //If so spinwait until no longer the case
                hasConflict = writeMask.AnyMatch(mask.WriteMask);
                var writeBits = mask.WriteMask.Bits;
                if (!hasConflict)
                {
                    //If none of the pools are being written to check if they are being read from
                    for (int i = 0; i < writeBits.Length; i++)
                    {
                        long val = writeBits[i];
                        for (int j = 0; j < 8 * sizeof(long); j++)
                        {
                            if (((val >>> j) & 1) == 1)
                            {
                                if (ComponentIdsMap.TryGetValue(i * 8 * sizeof(long) + j, out int index))
                                {
                                    hasConflict |= readAccessMask[index] != 0;
                                }
                            }
                        }
                    }
                    //If they are neither being read from nor being written to then exit
                    //and set them as written and set which components are has
                    if (!hasConflict)
                    {
                        poolAccessLock.EnterWriteLock();
                        var bitsHas = mask.HasMask.Bits;
                        for (int i = 0; i < bitsHas.Length; i++)
                        {
                            long val = bitsHas[i];
                            for (int j = 0; j < 8 * sizeof(long); j++)
                            {
                                if (((val >>> j) & 1) == 1)
                                {
                                    if (ComponentIdsMap.TryGetValue(i * 8 * sizeof(long) + j, out int index))
                                    {
                                        readAccessMask[index]++;
                                    }
                                }
                            }
                        }
                        writeMask.OrFilteredBits(mask.WriteMask, ComponentMask);
                        poolAccessLock.ExitWriteLock();
                    }
                }
                poolAccessLock.ExitUpgradeableReadLock();
            } while (hasConflict);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ReleaseAccess(ComponentMask mask)
        {
            poolAccessLock.EnterWriteLock();
            writeMask.ClearFilteredBits(mask.WriteMask, ComponentMask);
            var bitsHas = mask.HasMask.Bits;
            for (int i = 0; i < bitsHas.Length; i++)
            {
                long val = bitsHas[i];
                for (int j = 0; j < 8 * sizeof(long); j++)
                {
                    if (((val >>> j) & 1) == 1)
                    {
                        if (ComponentIdsMap.TryGetValue(i * 8 * sizeof(long) + j, out int index))
                        {
                            readAccessMask[index]--;
                        }
                    }
                }
            }
            poolAccessLock.ExitWriteLock();
        }

        public void Dispose()
        {
            ComponentIdsMap.Dispose();
            CommandBuffer.Dispose();
            poolAccessLock.Dispose();
            siblingAccessLock.Dispose();
            for (int i = 0; i < ComponentPools.Length; i++)
            {
                ComponentPools[i].Dispose();
            }
            EntitiesPool.Dispose();
        }
    }
}

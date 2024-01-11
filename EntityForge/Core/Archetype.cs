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
        internal readonly UnsafeSparseSet<int> componentIdsMap;
        internal readonly EcsCommandBuffer commandBuffer;
        internal readonly ReadOnlyMemory<ComponentInfo> componentInfo;
        internal readonly ArrayOrPointer[] componentPools;
        internal readonly ArrayOrPointer<Entity> entitiesPool;

        internal int elementCapacity;
        internal int elementCount;

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
            get
            {
                return elementCount;
            }
        }

        public Span<Entity> Entities
        {
            get
            {
                return entitiesPool.GetSpan(elementCount);
            }
        }

        public World World { get; }

        public Archetype(World world, ReadOnlyMemory<ComponentInfo> componentInfo, BitMask bitMask, int hash, int index)
        {
            World = world;
            commandBuffer = new(this);
            writeMask = new();
            readAccessMask = new int[componentInfo.Length];
            poolAccessLock = new();
            siblingAccessLock = new();
            ComponentMask = bitMask;
            Hash = hash;
            Index = index;
            Siblings = new();
            componentIdsMap = new();
            this.componentInfo = componentInfo;
            componentPools = new ArrayOrPointer[componentInfo.Length];
            elementCapacity = Archetype.DefaultPoolSize;
            var infos = componentInfo.Span;
            for (int i = 0; i < componentInfo.Length; i++)
            {
                ref readonly var compInfo = ref infos[i];
                componentIdsMap.Add(compInfo.TypeId, i);
                componentPools[i] = ArrayOrPointer.CreateForComponent(compInfo, elementCapacity);
                if (compInfo.IsUnmanaged)
                {
                    MemoryMarshal.CreateSpan(ref componentPools[i].GetFirst<byte>(), elementCapacity * infos[i].UnmanagedSize).Clear();
                }
            }

            entitiesPool = ArrayOrPointer<Entity>.Create(elementCapacity);
        }
        
        internal void AddEntityInternal(Entity entity)
        {
            GrowBy(1);
            entitiesPool.GetRefAt(elementCount++) = entity;
        }
     
        public void GrowBy(int added)
        {
            int desiredSize = elementCount + added;
            if (desiredSize >= elementCapacity)
            {
                GrowTo(desiredSize);
            }
        }

        void GrowTo(int desiredSize)
        {
            poolAccessLock.EnterWriteLock();
            int newCapacity = (int)BitOperations.RoundUpToPowerOf2((uint)desiredSize + 1);
            var infos = componentInfo.Span;
            for (int i = 0; i < componentPools.Length; i++)
            {
                ref var pool = ref componentPools[i];
                if (pool.IsUnmanaged)
                {
                    pool.GrowToUnmanaged(newCapacity, infos[i].UnmanagedSize);
                    MemoryMarshal.CreateSpan(ref pool.GetRefAt<byte>(elementCapacity * infos[i].UnmanagedSize), (newCapacity - elementCapacity) * infos[i].UnmanagedSize).Clear();
                }
                else
                {
                    pool.GrowToManaged(newCapacity, infos[i].Type!);
                }
            }
            entitiesPool.GrowTo(newCapacity);
            elementCapacity = newCapacity;
            poolAccessLock.ExitWriteLock();
        }

        
        internal void FillHole(int holeIndex)
        {
            poolAccessLock.EnterWriteLock();
            var infos = componentInfo.Span;
            //Swap last item with the removed item
            for (int i = 0; i < componentPools.Length; i++)
            {
                var pool = componentPools[i];
                if (pool.IsUnmanaged)
                {
                    pool.FillHoleUnmanaged(holeIndex, elementCount - 1, infos[i].UnmanagedSize);
                }
                else
                {
                    pool.FillHoleManaged(holeIndex, elementCount - 1);
                }
            }
            poolAccessLock.ExitWriteLock();
        }

        public Span<T> GetPool<T>(int index) where T : struct, IComponent<T>
        {
            ref var pool = ref componentPools[index];
            if (pool.IsUnmanaged)
            {
                return new Span<T>(pool.UnmanagedData, elementCount);
            }
            else
            {
                return new Span<T>((T[])pool.ManagedData!, 0, elementCount);
            }
        }

        public Span<T> GetPool<T>() where T : struct, IComponent<T>
        {
            ref var pool = ref componentPools[GetComponentIndex(World.GetOrCreateComponentId<T>())];
            if (pool.IsUnmanaged)
            {
                return new Span<T>(pool.UnmanagedData, elementCount);
            }
            else
            {
                return new Span<T>((T[])pool.ManagedData!, 0, elementCount);
            }
        }

        internal ref T GetRef<T>(int index) where T : struct, IComponent<T>
        {
            ref var pool = ref componentPools[GetComponentIndex(World.GetOrCreateComponentId<T>())];
            if (pool.IsUnmanaged)
            {
                return ref ((T*)pool.UnmanagedData)[index];
            }
            else
            {
                return ref ((T[])pool.ManagedData!)[index];
            }
        }

        
        internal void CopyComponents(int srcIndex, Archetype dest, int destIndex)
        {
            poolAccessLock.EnterReadLock();
            dest.poolAccessLock.EnterWriteLock();
            var infos = dest.componentInfo.Span;
            for (int i = 0; i < dest.componentInfo.Length; i++)
            {
                ref readonly var compInfo = ref infos[i];
                if (componentIdsMap.TryGetValue(compInfo.TypeId, out var index))
                {
                    if (compInfo.IsUnmanaged)
                    {
                        componentPools[index].CopyToUnmanaged(srcIndex, dest.componentPools[i].UnmanagedData, destIndex, compInfo.UnmanagedSize);
                    }
                    else
                    {
                        componentPools[index].CopyToManaged(srcIndex, dest.componentPools[i].ManagedData!, destIndex, 1);
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

        
        public int GetComponentIndex(int typeId)
        {
            return componentIdsMap.GetValue(typeId);
        }
    
        public ref T GetComponentByIndex<T>(int entityIndex, int compIndex) where T : struct, IComponent<T>
        {
            return ref (componentPools[compIndex].GetRefAt<T>(entityIndex));
        }
        
        public ref T GetComponent<T>(int entityIndex, int typeId) where T : struct, IComponent<T>
        {
            return ref (componentPools[GetComponentIndex(typeId)].GetRefAt<T>(entityIndex));
        }
        
        public ref T GetComponent<T>(int entityIndex) where T : struct, IComponent<T>
        {
            return ref GetComponent<T>(entityIndex, World.GetOrCreateComponentId<T>());
        }

        
        public void SetComponent(int entityIndex, ComponentInfo info, object data)
        {
            var pool = componentPools[GetComponentIndex(info.TypeId)];
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

        
        public bool HasComponent<T>() where T : struct, IComponent<T>
        {
            return HasComponent(World.GetOrCreateComponentId<T>());
        }

        
        public bool TryGetComponentIndex<T>(out int index) where T : struct, IComponent<T>
        {
            return componentIdsMap.TryGetValue(World.GetOrCreateComponentId<T>(), out index);
        }

        
        public bool HasComponent(int id)
        {
            return componentIdsMap.Has(id);
        }

        /// <summary>
        /// Blocks adding or removing components & entities until a later time
        /// </summary>
        
        public void Lock()
        {
            int i = Interlocked.Increment(ref lockCount);
        }

        
        public void Unlock()
        {
            var val = Interlocked.Decrement(ref lockCount);
            if (val == 0)
            {
                commandBuffer.OnUnlock();
            }
        }
        /// <summary>
        /// Tries to get access to the pools defined by this mask
        /// </summary>
        /// <param name="mask"></param>
        
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
                                if (componentIdsMap.TryGetValue(i * 8 * sizeof(long) + j, out int index))
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
                                    if (componentIdsMap.TryGetValue(i * 8 * sizeof(long) + j, out int index))
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

        
        public void ReleaseAccess(ComponentMask mask)
        {
            poolAccessLock.EnterWriteLock();
            writeMask.ClearMatchingBits(mask.WriteMask, ComponentMask);
            var bitsHas = mask.HasMask.Bits;
            for (int i = 0; i < bitsHas.Length; i++)
            {
                long val = bitsHas[i];
                for (int j = 0; j < 8 * sizeof(long); j++)
                {
                    if (((val >>> j) & 1) == 1)
                    {
                        if (componentIdsMap.TryGetValue(i * 8 * sizeof(long) + j, out int index))
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
            componentIdsMap.Dispose();
            commandBuffer.Dispose();
            poolAccessLock.Dispose();
            siblingAccessLock.Dispose();
            for (int i = 0; i < componentPools.Length; i++)
            {
                componentPools[i].Dispose();
            }
            entitiesPool.Dispose();
        }
    }
}

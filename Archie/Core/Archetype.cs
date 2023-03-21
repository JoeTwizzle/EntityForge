using CommunityToolkit.HighPerformance;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Archie
{
    public unsafe sealed class Archetype : IDisposable
    {
        public const int DefaultPoolSize = 8;
        /// <summary>
        /// BitMask of which components this archtype contains
        /// </summary>
        internal readonly BitMask BitMask;
        /// <summary>
        /// Unique Index of this Archetype
        /// </summary>
        public readonly int Index;
        /// <summary>
        /// Unique ID of this Archetype
        /// </summary>
        public readonly int Hash;
        /// <summary>
        /// Connections to Archetypes differing by only one component
        /// </summary>
        internal readonly Dictionary<ComponentId, ArchetypeSiblings> Siblings;
        internal readonly ComponentInfo[] ComponentInfo;
        internal readonly ArrayOrPointer[] PropertyPools;
        internal ArrayOrPointer EntitiesPool;
        internal int PropertyCount => PropertyPools.Length;
        internal int ElementCapacity;
        internal int ElementCount;
        /// <summary>
        /// Maps at which index components of a given typeid are stored
        /// </summary>
        //TODO: make FrozenDict when its added
        internal readonly Dictionary<ComponentId, int> ComponentIdsMap;

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
                return new Span<Entity>(EntitiesPool.UnmanagedData, ElementCount);
            }
        }

        internal Span<Entity> EntityBuffer
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new Span<Entity>(EntitiesPool.UnmanagedData, ElementCapacity);
            }
        }

        public Archetype(ComponentInfo[] componentInfo, BitMask bitMask, int hash, int index)
        {
            BitMask = bitMask;
            Hash = hash;
            Index = index;
            Siblings = new();
            ComponentIdsMap = new();
            ComponentInfo = componentInfo;
            PropertyPools = new ArrayOrPointer[componentInfo.Length];
            for (int i = 0; i < componentInfo.Length; i++)
            {
                ref var compInfo = ref componentInfo[i];
                ComponentIdsMap.Add(compInfo.ComponentId, i);
                if (compInfo.Type == null)
                {
                    PropertyPools[i] = new ArrayOrPointer(NativeMemory.Alloc((nuint)Archetype.DefaultPoolSize, (nuint)compInfo.UnmanagedSize));
                }
                else
                {
                    PropertyPools[i] = new ArrayOrPointer(Array.CreateInstance(compInfo.Type, Archetype.DefaultPoolSize));
                }
            }
            EntitiesPool = new ArrayOrPointer(NativeMemory.Alloc((nuint)Archetype.DefaultPoolSize, (nuint)Unsafe.SizeOf<Entity>()));
            ElementCapacity = Archetype.DefaultPoolSize;
        }

        public void GrowIfNeeded(int added)
        {
            int desiredSize = ElementCount + added;
            if (desiredSize >= ElementCapacity)
            {
                int newCapacity = ElementCapacity;
                do
                {
                    newCapacity *= 2;
                }
                while (desiredSize >= newCapacity);

                for (int i = 0; i < PropertyPools.Length; i++)
                {
                    ref var pool = ref PropertyPools[i];
                    if (pool.IsUnmanaged)
                    {
                        pool.GrowToUnmanaged(newCapacity, ComponentInfo[i].UnmanagedSize);
                    }
                    else
                    {
                        pool.GrowToManaged(newCapacity, ComponentInfo[i].Type!);
                    }
                }

                EntitiesPool.GrowToUnmanaged(newCapacity, Unsafe.SizeOf<Entity>());
                ElementCapacity = newCapacity;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void FillHole(int holeIndex)
        {
            //Swap last item with the removed item
            for (int i = 0; i < PropertyPools.Length; i++)
            {
                var pool = PropertyPools[i];
                if (pool.IsUnmanaged)
                {
                    pool.FillHoleUnmanaged(holeIndex, ElementCount - 1, ComponentInfo[i].UnmanagedSize);
                }
                else
                {
                    pool.FillHoleManaged(holeIndex, ElementCount - 1);
                }
            }
        }

        public Span<T> GetPool<T>(int variant = 0) where T : struct, IComponent<T>
        {
            ref var pool = ref PropertyPools[GetComponentIndex(World.GetOrCreateComponentId<T>(variant))];
            if (pool.IsUnmanaged)
            {
                return new Span<T>(pool.UnmanagedData, ElementCount);
            }
            else
            {
                return new Span<T>((T[])pool.ManagedData!, 0, ElementCount);
            }
        }

        public Span<T> GetPoolUnsafe<T>(int variant = 0) where T : struct, IComponent<T>
        {
            ref var pool = ref PropertyPools[GetComponentIndex(World.GetOrCreateComponentId<T>(variant))];
            if (pool.IsUnmanaged)
            {
                return new Span<T>(pool.UnmanagedData, ElementCapacity);
            }
            else
            {
                return new Span<T>((T[])pool.ManagedData!, 0, ElementCapacity);
            }
        }

        internal ref T GetRef<T>(int index, int variant = 0) where T : struct, IComponent<T>
        {
            ref var pool = ref PropertyPools[GetComponentIndex(World.GetOrCreateComponentId<T>(variant))];
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
            for (int i = 0; i < dest.ComponentInfo.Length; i++)
            {
                ref var compInfo = ref dest.ComponentInfo[i];
                if (ComponentIdsMap.TryGetValue(compInfo.ComponentId, out var index))
                {
                    if (compInfo.IsUnmanaged)
                    {
                        PropertyPools[index].CopyToUnmanaged(srcIndex, dest.PropertyPools[i].UnmanagedData, destIndex, compInfo.UnmanagedSize);
                    }
                    else
                    {
                        PropertyPools[index].CopyToManaged(srcIndex, dest.PropertyPools[i].ManagedData!, destIndex, 1);
                    }
                }
            }
        }

        #region Siblings

        public ArchetypeSiblings SetSiblingAdd(ComponentId component, Archetype sibling)
        {
            if (!Siblings.TryGetValue(component, out var archetypes))
            {
                archetypes = new ArchetypeSiblings(sibling, null);
                Siblings.Add(component, archetypes);
                return archetypes;
            }
            archetypes.Add = sibling;
            Siblings[component] = archetypes;
            return archetypes;
        }

        public ArchetypeSiblings SetSiblingRemove(ComponentId component, Archetype sibling)
        {
            if (!Siblings.TryGetValue(component, out var archetypes))
            {
                archetypes = new ArchetypeSiblings(null, sibling);
                Siblings.Add(component, archetypes);
                return archetypes;
            }
            archetypes.Remove = sibling;
            Siblings[component] = archetypes;
            return archetypes;
        }

        public bool HasSiblingAdd(ComponentId component)
        {
            if (!Siblings.TryGetValue(component, out var archetypes))
            {
                return false;
            }
            return archetypes.Add != null;
        }

        public bool HasSiblingRemove(ComponentId component)
        {
            if (!Siblings.TryGetValue(component, out var archetypes))
            {
                return false;
            }
            return archetypes.Remove != null;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetSiblingAdd(ComponentId component, [NotNullWhen(true)] out Archetype? siblingAdd)
        {
            if (!Siblings.TryGetValue(component, out var archetypes))
            {
                siblingAdd = null;
                return false;
            }
            siblingAdd = archetypes.Add;
            return siblingAdd != null;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetSiblingRemove(ComponentId component, [NotNullWhen(true)] out Archetype? siblingRemove)
        {
            if (!Siblings.TryGetValue(component, out var archetypes))
            {
                siblingRemove = null;
                return false;
            }
            siblingRemove = archetypes.Remove;
            return siblingRemove != null;
        }

        #endregion

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetComponentIndex(ComponentId compInfo)
        {
            return ComponentIdsMap[compInfo];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T GetComponentByIndex<T>(int entityIndex, int compIndex) where T : struct, IComponent<T>
        {
            return ref (PropertyPools[compIndex].GetRefAt<T>(entityIndex));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T GetComponent<T>(int entityIndex, ComponentId compInfo) where T : struct, IComponent<T>
        {
            return ref (PropertyPools[GetComponentIndex(compInfo)].GetRefAt<T>(entityIndex));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T GetComponent<T>(int entityIndex, int variant = World.DefaultVariant) where T : struct, IComponent<T>
        {
            return ref GetComponent<T>(entityIndex, new ComponentId(T.Id, variant));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasComponent<T>(int variant = World.DefaultVariant) where T : struct, IComponent<T>
        {
            return HasComponent(World.GetOrCreateComponentId<T>(variant));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasComponent(ComponentId id)
        {
            return ComponentIdsMap.ContainsKey(id);
        }

        public void Dispose()
        {
            for (int i = 0; i < PropertyPools.Length; i++)
            {
                if (PropertyPools[i].IsUnmanaged)
                {
                    NativeMemory.Free(PropertyPools[i].UnmanagedData);
                }
                else
                {
                    PropertyPools[i].ManagedData = null;
                }
            }
            NativeMemory.Free(EntitiesPool.UnmanagedData);
        }

        internal static bool Contains(Archetype archetype, ComponentId type)
        {
            return archetype.ComponentIdsMap.ContainsKey(type);
        }
    }
}

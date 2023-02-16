using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

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

    public sealed class Archetype : IEquatable<Archetype>
    {
        private const int DefaultPoolSize = 8;
        /// <summary>
        /// Unique Index of this Archetype
        /// </summary>
        public readonly int Index;
        /// <summary>
        /// Unique ID of this Archetype
        /// </summary>
        public readonly int Hash;
        /// <summary>
        /// BitMask of which components this archtype contains
        /// </summary>
        internal readonly BitMask BitMask;
        /// <summary>
        /// Array of Component Arrays
        /// </summary>
        internal readonly Array[] PropertyPool;
        /// <summary>
        /// ComponentIds Stored in PropertyPool
        /// </summary>
        internal readonly ComponentId[] Components;
        /// <summary>
        /// ComponentIds of Components Stored in PropertyPool
        /// </summary>
        internal ReadOnlySpan<ComponentId> ComponentTypes => Components;
        /// <summary>
        /// Connections to Archetypes differing by only one component
        /// </summary>
        internal readonly Dictionary<ComponentId, ArchetypeSiblings> Siblings;
        /// <summary>
        /// Maps at which index components of a given typeid are stored
        /// </summary>
        //TODO: make FrozenDict when its added
        internal readonly Dictionary<ComponentId, int> ComponentIdsMap;
        /// <summary>
        /// Number of Entities
        /// </summary>
        internal int InternalEntityCount;
        /// <summary>
        /// Defines whether we are forbidden to mutate this Archetype (Order only)
        /// </summary>
        internal bool Locked;

        internal Span<EntityId> EntitiesBuffer
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
            get
            {
                return new Span<EntityId>((EntityId[])PropertyPool[PropertyPool.Length - 1]);
            }
        }

        public Span<EntityId> Entities
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
            get
            {
                return new Span<EntityId>((EntityId[])PropertyPool[PropertyPool.Length - 1], 0, InternalEntityCount);
            }
        }

        public int EntityCount
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
            get
            {
                return InternalEntityCount;
            }
        }

        public Archetype(ComponentId[] componentIds, BitMask bitMask, int hash, int index)
        {
            //Init PropertyPool
            BitMask = bitMask;
            Hash = hash;
            InternalEntityCount = 0;
            Index = index;
            ComponentIdsMap = new(componentIds.Length);
            Siblings = new();
            Components = componentIds;
            PropertyPool = new Array[componentIds.Length + 1];
            for (int i = 0; i < componentIds.Length; i++)
            {
                ComponentIdsMap.Add(componentIds[i], i);
                PropertyPool[i] = Array.CreateInstance(componentIds[i].Type, DefaultPoolSize);
            }
            PropertyPool[componentIds.Length] = Array.CreateInstance(typeof(EntityId), DefaultPoolSize);
        }

        #region Resizing

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void GrowIfNeeded(int added)
        {
            int sum = InternalEntityCount + added;
            int compCount = (int)PropertyPool.Length;
            if (compCount > 0)
            {
                int length = (int)PropertyPool[0].Length;
                if (length < sum)
                {
                    //Grow by 2x
                    int newCapacity = length * 2;
                    //Keep doubling size if we grow by a large amount
                    while (newCapacity < sum)
                    {
                        newCapacity *= 2;
                    }
                    if (newCapacity > Array.MaxLength) newCapacity = (int)Array.MaxLength;
                    for (int idx = 0; idx < Components.Length; idx++)
                    {
                        var old = PropertyPool[idx];
                        var newPool = Array.CreateInstance(Components[idx].Type, newCapacity);
                        PropertyPool[idx] = newPool;
                        //move existing entities
                        Array.Copy(old, 0, newPool, 0, InternalEntityCount);
                    }
                    var oldEnt = PropertyPool[Components.Length];
                    var newPoolEnt = Array.CreateInstance(typeof(EntityId), newCapacity);
                    PropertyPool[Components.Length] = newPoolEnt;
                    //move existing entities
                    Array.Copy(oldEnt, 0, newPoolEnt, 0, InternalEntityCount);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Reset()
        {
            for (int idx = 0; idx < PropertyPool.Length; idx++)
            {
                PropertyPool[idx] = Array.CreateInstance(Components[idx].Type, DefaultPoolSize);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void FillHole(int holeIndex)
        {
            //Swap last item with the removed item
            for (int i = 0; i < PropertyPool.Length; i++)
            {
                var pool = PropertyPool[i];
                Array.Copy(pool, InternalEntityCount - 1, pool, holeIndex, 1);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void CopyComponents(int srcIndex, Archetype dest, int destIndex)
        {
            for (int i = 0; i < dest.Components.Length; i++)
            {
                if (ComponentIdsMap.TryGetValue(dest.Components[i], out var index))
                {
                    Array.Copy(PropertyPool[index], srcIndex, dest.PropertyPool[i], destIndex, 1);
                }
            }
        }

        #endregion

        public static bool Contains(Archetype archetype, ComponentId type)
        {
            return GetIndex(archetype, type) >= 0;
        }

        public static int GetIndex(Archetype archetype, ComponentId type)
        {
            return archetype.ComponentTypes.BinarySearch(type, new TypeComparer());
        }

        #region Accessors

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetComponentIndex(int componentId, int variant = World.DefaultVariant)
        {
            return GetComponentIndex(new ComponentId(componentId, variant, null!));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetComponentIndex(ComponentId component)
        {
            return ComponentIdsMap[component];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<T> GetPool<T>(int variant = World.DefaultVariant) where T : struct, IComponent<T>
        {
            return new Span<T>(((T[])PropertyPool[ComponentIdsMap[new ComponentId(T.Id, variant, typeof(T))]]), 0, InternalEntityCount);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T[] DangerousGetPool<T>(int variant = World.DefaultVariant) where T : struct, IComponent<T>
        {
            return ((T[])PropertyPool[ComponentIdsMap[new ComponentId(T.Id, variant, typeof(T))]]);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Array DangerousGetPool(ComponentId component)
        {
            return PropertyPool[ComponentIdsMap[component]];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Array DangerousGetPool(int index)
        {
            return PropertyPool[index];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T GetComponent<T>(int entityIndex, ComponentId compId) where T : struct, IComponent<T>
        {
            return ref ((T[])PropertyPool[GetComponentIndex(compId)])[entityIndex];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T GetComponent<T>(int entityIndex, int variant = World.DefaultVariant) where T : struct, IComponent<T>
        {
            return ref ((T[])PropertyPool[GetComponentIndex(T.Id, variant)])[entityIndex];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasComponent<T>(int variant = World.DefaultVariant) where T : struct, IComponent<T>
        {
            return ComponentIdsMap.ContainsKey(new ComponentId(T.Id, variant, typeof(T)));
        }

        #endregion

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

        public bool Equals(Archetype? other)
        {
            return other?.Hash == Hash;
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as Archetype);
        }

        public override int GetHashCode()
        {
            return Hash;
        }
    }
}
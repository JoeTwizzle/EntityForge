using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Archie
{
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
        /// Types Stored in PropertyPool
        /// </summary>
        internal readonly (Type, int)[] PropertyTypes;
        /// <summary>
        /// Ids of Components Stored in PropertyPool
        /// </summary>
        internal readonly int[] ComponentTypeIds;
        /// <summary>
        /// Types of Components Stored in PropertyPool
        /// </summary>
        internal ReadOnlySpan<(Type, int)> ComponentTypes => new ReadOnlySpan<(Type, int)>(PropertyTypes, 0, ComponentCount);
        /// <summary>
        /// Types of non-Components Stored
        /// </summary>
        internal ReadOnlySpan<(Type, int)> OtherTypes => new ReadOnlySpan<(Type, int)>(PropertyTypes, ComponentCount, OtherTypesCount);
        /// <summary>
        /// Connections to Archetypes differing by only one component
        /// </summary>
        internal readonly Dictionary<int, ArchetypeSiblings> Siblings;
        /// <summary>
        /// Maps at which index components of a given type are stored
        /// </summary>
        internal readonly Dictionary<Type, int> TypeMap;
        /// <summary>
        /// Maps at which index components of a given typeid are stored
        /// </summary>
        internal readonly Dictionary<int, int> TypeIdsMap;
        /// <summary>
        /// Number of Entities
        /// </summary>
        internal int InternalEntityCount;
        /// <summary>
        /// Number of components
        /// </summary>
        internal readonly int ComponentCount;
        /// <summary>
        /// Number of other types
        /// </summary>
        internal readonly int OtherTypesCount;
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

        public Archetype(int[] componentIds, (Type, int)[] componentTypes, (Type, int)[] otherTypes, BitMask bitMask, int hash, int index)
        {
            ComponentTypeIds = componentIds;

            //Init PropertyPool
            ComponentCount = componentTypes.Length;
            OtherTypesCount = otherTypes.Length;
            BitMask = bitMask;
            Hash = hash;
            InternalEntityCount = 0;
            Index = index;
            TypeMap = new(ComponentCount);
            TypeIdsMap = new(ComponentCount);
            Siblings = new Dictionary<int, ArchetypeSiblings>();
            PropertyTypes = new (Type, int)[ComponentCount + OtherTypesCount];
            Array.Copy(componentTypes, PropertyTypes, ComponentCount);
            Array.Copy(otherTypes, 0, PropertyTypes, ComponentCount, OtherTypesCount);
            PropertyPool = new Array[ComponentCount + OtherTypesCount];
            for (int i = 0; i < componentTypes.Length; i++)
            {
                TypeIdsMap.Add(componentIds[i], i);
                TypeMap.Add(componentTypes[i].Item1, i);
                PropertyPool[i] = Array.CreateInstance(componentTypes[i].Item1, DefaultPoolSize);
            }
            for (int i = 0; i < OtherTypes.Length; i++)
            {
                PropertyPool[ComponentCount + i] = Array.CreateInstance(OtherTypes[i].Item1, DefaultPoolSize);
            }
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
                    for (int idx = 0; idx < PropertyPool.Length; idx++)
                    {
                        var old = PropertyPool[idx];
                        var newPool = Array.CreateInstance(PropertyTypes[idx].Item1, newCapacity);
                        PropertyPool[idx] = newPool;
                        //move existing entities
                        Array.Copy(old, 0, newPool, 0, InternalEntityCount);
                    }
                }
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
            for (int i = 0; i < dest.ComponentTypeIds.Length; i++)
            {
                if (TypeIdsMap.TryGetValue(dest.ComponentTypeIds[i], out var index))
                {
                    Array.Copy(PropertyPool[index], srcIndex, dest.PropertyPool[i], destIndex, 1);
                }
            }
        }

        #endregion

        #region Accessors

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetComponentIndex(Type type)
        {
            return TypeMap[type];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetComponentIndex<T>()
        {
            return TypeMap[typeof(T)];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<T> GetPool<T>()
        {
            return new Span<T>(((T[])PropertyPool[TypeMap[typeof(T)]]), 0, InternalEntityCount);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T[] DangerousGetPool<T>()
        {
            return ((T[])PropertyPool[TypeMap[typeof(T)]]);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Array DangerousGetPool(Type type)
        {
            return PropertyPool[TypeMap[type]];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Array DangerousGetPool(int index)
        {
            return PropertyPool[index];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T GetComponent<T>(int index)
        {
            return ref ((T[])PropertyPool[TypeMap[typeof(T)]])[index];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T GetComponent<T>(int index, int typeId)
        {
            return ref ((T[])PropertyPool[TypeIdsMap[typeId]])[index];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasComponent<T>()
        {
            return TypeMap.ContainsKey(typeof(T));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasComponent(Type type)
        {
            return TypeMap.ContainsKey(type);
        }

        #endregion

        #region Siblings

        public ArchetypeSiblings SetSiblingAdd(int component, Archetype sibling)
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

        public ArchetypeSiblings SetSiblingRemove(int component, Archetype sibling)
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

        public bool HasSiblingAdd(int component)
        {
            if (!Siblings.TryGetValue(component, out var archetypes))
            {
                return false;
            }
            return archetypes.Add != null;
        }

        public bool HasSiblingRemove(int component)
        {
            if (!Siblings.TryGetValue(component, out var archetypes))
            {
                return false;
            }
            return archetypes.Remove != null;
        }

        public bool TryGetSiblingAdd(int component, [NotNullWhen(true)] out Archetype? siblingAdd)
        {
            if (!Siblings.TryGetValue(component, out var archetypes))
            {
                siblingAdd = null;
                return false;
            }
            siblingAdd = archetypes.Add;
            return siblingAdd != null;
        }

        public bool TryGetSiblingRemove(int component, [NotNullWhen(true)] out Archetype? siblingRemove)
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
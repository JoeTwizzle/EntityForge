
using System;
using System.Buffers;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;

namespace Archie
{
    public sealed class Archetype : IEquatable<Archetype>
    {
        const int DefaultPoolSize = 256;
        /// <summary>
        /// Unique ID of this Archetype
        /// </summary>
        public readonly ArchitypeId Id;
        /// <summary>
        /// Array of Component Arrays
        /// </summary>
        public readonly Array[] ComponentPools;
        /// <summary>
        /// Array of Component Arrays
        /// </summary>
        public readonly Type[] Types;
        /// <summary>
        /// Connections to Archetypes differing by only one component
        /// </summary>
        public Dictionary<Type, ArchetypeSiblings> Siblings;
        /// <summary>
        /// Number of Entities
        /// </summary>
        internal uint entityCount;
        public uint EntityCount => entityCount;

        /// <summary>
        /// Sorts and remove duplicates form an archetype definition
        /// </summary>
        /// <param name="components"></param>
        /// <returns></returns>
        public static ArchetypeDefinition CreateDefinition(params Type[] components)
        {
            World.SortTypes(components);
            components = RemoveDuplicates(components);
            return new ArchetypeDefinition(World.GetComponentHash(components), components);
        }

        private static Type[] RemoveDuplicates(Type[] types)
        {
            int head = 0;
            Span<int> indices = types.Length < 512 ? stackalloc int[types.Length] : new int[types.Length];
            Guid prevType = Guid.Empty;
            for (int i = 0; i < types.Length; i++)
            {
                //This only works if the array is sorted
                if (prevType == types[i].GUID)
                {
                    continue;
                }
                indices[head++] = i;
            }
            //Contained no duplicates
            if (head == types.Length)
            {
                return types;
            }
            var deDup = new Type[head];
            for (int i = 0; i < deDup.Length; i++)
            {
                deDup[i] = types[indices[--head]];
            }
            return deDup;
        }

        public Archetype(Type[] components, int hash)
        {
            Id = new ArchitypeId(hash);
            Types = components;
            ComponentPools = new Array[components.Length];
            for (int i = 0; i < components.Length; i++)
            {
                ComponentPools[i] = Array.CreateInstance(components[i], DefaultPoolSize);
            }
            Siblings = new Dictionary<Type, ArchetypeSiblings>();
            entityCount = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void GrowIfNeeded(uint added)
        {
            uint sum = entityCount + added;
            uint compCount = (uint)ComponentPools.Length;
            if (compCount > 0)
            {
                uint length = (uint)ComponentPools[0].Length;
                if (length < sum)
                {
                    for (int idx = 0; idx < ComponentPools.Length; idx++)
                    {
                        var old = ComponentPools[idx];
                        //Grow by 2x
                        uint newCapacity = length * 2;
                        //Keep doubling size if we grow by a large amount
                        while (newCapacity < sum)
                        {
                            newCapacity *= 2u;
                        }
                        if (newCapacity > Array.MaxLength) newCapacity = (uint)Array.MaxLength;
                        var newPool = Array.CreateInstance(Types[idx], newCapacity);
                        ComponentPools[idx] = newPool;
                        //move existing entities
                        Array.Copy(old, 0, newPool, 0, entityCount);
                    }
                }
            }
        }

        #region Siblings

        public ArchetypeSiblings SetSiblingAdd(Type component, Archetype sibling)
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

        public ArchetypeSiblings SetSiblingRemove(Type component, Archetype sibling)
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

        public bool HasSiblingAdd(Type component)
        {
            if (!Siblings.TryGetValue(component, out var archetypes))
            {
                return false;
            }
            return archetypes.Add != null;
        }

        public bool HasSiblingRemove(Type component)
        {
            if (!Siblings.TryGetValue(component, out var archetypes))
            {
                return false;
            }
            return archetypes.Remove != null;
        }

        public bool TryGetSiblingAdd(Type component, [NotNullWhen(true)] out Archetype? siblingAdd)
        {
            if (!Siblings.TryGetValue(component, out var archetypes))
            {
                siblingAdd = null;
                return false;
            }
            siblingAdd = archetypes.Add;
            return siblingAdd != null;
        }

        public bool TryGetSiblingRemove(Type component, [NotNullWhen(true)] out Archetype? siblingRemove)
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
            return other?.Id == Id;
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as Archetype);
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
    }
}

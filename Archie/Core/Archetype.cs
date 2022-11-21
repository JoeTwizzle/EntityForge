using System;
using System.Buffers;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
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

        public Archetype(Type[] components)
        {
            Id = new ArchitypeId(World.GetComponentHash(components));
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
        internal void GrowIfNeeded(int added)
        {
            for (int idx = 0; idx < ComponentPools.Length; idx++)
            {
                if ((entityCount + added) >= (ComponentPools[idx].Length - 1))
                {
                    var old = ComponentPools[idx];
                    //Grow by 2x
                    int newCapacity = old.Length * 2;
                    //Keep doubling size if we grow by a large amount
                    while (newCapacity < entityCount + added) 
                    {
                        newCapacity *= 2;
                    }
                    if ((uint)newCapacity > Array.MaxLength) newCapacity = Array.MaxLength;
                    var newPool = Array.CreateInstance(Types[idx], newCapacity);
                    ComponentPools[idx] = newPool;
                    //move existing entities
                    old.CopyTo(newPool, 0);
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

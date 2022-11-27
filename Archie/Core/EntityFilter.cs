using Archie.Helpers;
using CommunityToolkit.HighPerformance;
using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Archie
{
    public sealed class EntityFilter
    {
        World world;
        List<uint> archetypes = new List<uint>();
        public int ArchetypeCount => archetypes.Count;

        internal unsafe EntityFilter(World world, ComponentMask mask)
        {
            this.world = world;
            if (mask.Included.Length > 0)  //Add archetypes matching condition 1
            {
                archetypes.AddRange(world.GetContainingArchetypes(mask.Included[0]));
                for (int i = 1; i < mask.Included.Length; i++)
                {
                    var containing = world.GetContainingArchetypes(mask.Included[i]).AsSpan();
                    for (int j = archetypes.Count - 1; j >= 0; j--)
                    {
                        // If we are not matching 
                        if (!containing.Contains(archetypes[j]))
                        {
                            archetypes.RemoveAt(j);
                        }
                    }
                }
            }
            else //Add all archetypes
            {
                int length = (int)world.ArchtypeCount;
                var array = ArrayPool<uint>.Shared.Rent((int)world.ArchtypeCount);
                for (uint i = 0; i < length; i++)
                {
                    array[i] = i;
                }
                archetypes.AddRange(new ArraySegment<uint>(array, 0, length));
                ArrayPool<uint>.Shared.Return(array);
            }
            for (int i = 0; i < mask.Excluded.Length; i++)
            {
                var containing = world.GetContainingArchetypes(mask.Excluded[i]).AsSpan();
                for (int j = archetypes.Count - 1; j >= 0; j--)
                {
                    // If we are not matching 
                    if (containing.Contains(archetypes[j]))
                    {
                        archetypes.RemoveAt(j);
                    }
                }
            }
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        public ref struct Enumerator
        {
            World world;
            List<uint> archetypes;
            int lockCount;
            int archetypeIndex;
            int entityIndex;
            public Enumerator(EntityFilter filter)
            {
                world = filter.world;
                archetypes = filter.archetypes;
                archetypeIndex = 0;
                entityIndex = -1;
            }

            public Enumerator(World world, List<uint> archetypes)
            {
                this.world = world;
                this.archetypes = archetypes;
            }

            public uint CurrentArchetypeIndex => archetypes[archetypeIndex];

            public EntityId Current => world.Entities[CurrentArchetypeIndex].Entities[entityIndex];

            public void Dispose()
            {
                lockCount--;
            }

            public bool MoveNext()
            {
                ++entityIndex;
                if (entityIndex >= world.Entities[CurrentArchetypeIndex].Entities.Count)
                {
                    entityIndex = 0;
                    ++archetypeIndex;
                }
                return archetypeIndex < archetypes.Count;
            }

            public void Reset()
            {
                archetypeIndex = -1;
            }
        }
    }
}

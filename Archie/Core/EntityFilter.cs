using Archie.Helpers;
using CommunityToolkit.HighPerformance;
using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Archie.EntityFilter;

namespace Archie
{
    public sealed class EntityFilter
    {
        World world;
        BitMask incMask;
        BitMask excMask;

        internal EntityFilter(World world, ComponentMask mask)
        {
            this.world = world;
            excMask = new BitMask();
            for (int i = 0; i < mask.Excluded.Length; i++)
            {
                excMask.SetBit((int)world.GetComponentID(mask.Excluded[i]));
            }
            incMask = new BitMask();
            for (int i = 0; i < mask.Included.Length; i++)
            {
                incMask.SetBit((int)world.GetComponentID(mask.Included[i]));
            }
        }

        public EntityEnumerator GetEnumerator()
        {
            return new EntityEnumerator(this);
        }

        public ref struct ArchetypeEnumerator
        {
            BitMask incMask;
            BitMask excMask;
            World world;
            int currentArchetype;

            public ArchetypeEnumerator(EntityFilter filter)
            {
                world = filter.world;
                incMask = filter.incMask;
                excMask = filter.excMask;
                currentArchetype = -1;
            }
            public Archetype Current => world.AllArchetypes.DangerousGetReferenceAt(currentArchetype);

            public bool MoveNext()
            {
                do
                {
                    ++currentArchetype;
                } while (currentArchetype < world.ArchtypeCount && !(incMask.AllMatch(Current.BitMask) && !excMask.AnyMatch(Current.BitMask)));
                return currentArchetype < world.ArchtypeCount;
            }

            public void Reset()
            {
                currentArchetype = -1;
            }
        }

        public ref struct EntityEnumerator
        {
            BitMask incMask;
            BitMask excMask;
            World world;
            int currentEntity;
            ArchetypeEnumerator archetypeEnumerator;

            public EntityEnumerator(EntityFilter filter)
            {
                archetypeEnumerator = new(filter);
                world = filter.world;
                incMask = filter.incMask;
                excMask = filter.excMask;
                currentEntity = -1; 
                archetypeEnumerator.MoveNext();
            }
            public EntityId Current => world.Entities[archetypeEnumerator.Current.Index].Entities.DangerousGetReferenceAt(currentEntity);

            public bool MoveNext()
            {
                bool ok = ++currentEntity < archetypeEnumerator.Current.entityCount;
                if (!ok)
                {
                    ok = archetypeEnumerator.MoveNext();
                    currentEntity = 0;
                }
                return ok;
            }

            public void Reset()
            {
                archetypeEnumerator.Reset();
                currentEntity = -1;
            }
        }
    }
}

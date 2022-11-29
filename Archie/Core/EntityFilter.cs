using Archie.Helpers;
using CommunityToolkit.HighPerformance;
using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Archie
{
    public sealed class EntityFilter
    {
        internal readonly World world;
        internal readonly BitMask incMask;
        internal readonly BitMask excMask;

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Matches(BitMask mask)
        {
            return incMask.AllMatch(mask) && !excMask.AnyMatch(mask);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityEnumerator GetEnumerator()
        {
            return new EntityEnumerator(this);
        }

        public ref struct ArchetypeEnumerator
        {
            BitMask incMask;
            BitMask excMask;
            World world;
            int currentArchetypeIndex;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ArchetypeEnumerator(EntityFilter filter)
            {
                world = filter.world;
                incMask = filter.incMask;
                excMask = filter.excMask;
                currentArchetypeIndex = -1;
            }

            public Archetype Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    return world.AllArchetypes[currentArchetypeIndex];
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                do
                {
                    ++currentArchetypeIndex;
                } while (currentArchetypeIndex < world.ArchtypeCount && !(incMask.AllMatch(Current.BitMask) && !excMask.AnyMatch(Current.BitMask)));
                return currentArchetypeIndex < world.ArchtypeCount;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Reset()
            {
                currentArchetypeIndex = -1;
            }
        }

        public ref struct EntityEnumerator
        {
            int currentEntity;
            Archetype? currentArchetype;
            ArchetypeEnumerator archetypeEnumerator;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public EntityEnumerator(EntityFilter filter)
            {
                archetypeEnumerator = new(filter);
                currentEntity = -1;
                if (archetypeEnumerator.MoveNext())
                {
                    currentArchetype = archetypeEnumerator.Current;
                }
            }

            public EntityId Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    return currentArchetype!.Entities[currentEntity];
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                if (currentArchetype == null)
                {
                    return false;
                }
                if (++currentEntity >= currentArchetype.entityCount)
                {
                    currentEntity = 0;
                    return archetypeEnumerator.MoveNext();
                }
                return true;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Reset()
            {
                archetypeEnumerator.Reset();
                currentArchetype = null;
                currentEntity = -1;
            }
        }
    }
}

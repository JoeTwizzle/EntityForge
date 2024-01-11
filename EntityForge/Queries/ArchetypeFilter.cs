using EntityForge.Collections;
using EntityForge.Collections.Generic;
using System.Runtime.CompilerServices;

namespace EntityForge.Queries
{
    public sealed class ArchetypeFilter : IDisposable
    {
        internal readonly World world;
        internal readonly ComponentMask componentMask;

        public ReadOnlySpan<Archetype> MatchingArchetypes
        {
            
            get
            {
                return MatchingArchetypesBuffer.GetDenseData();
            }
        }

        UnsafeSparseSet<Archetype> MatchingArchetypesBuffer;
        public int MatchCount => MatchingArchetypesBuffer.DenseCount;

        internal ArchetypeFilter(World world, ComponentMask mask)
        {
            this.world = world;
            this.componentMask = mask;
            MatchingArchetypesBuffer = new();
            world.worldArchetypesRWLock.EnterReadLock();
            var archetypes = world.Archetypes;
            for (int i = 0; i < archetypes.Length; i++)
            {
                if (Matches(archetypes[i].ComponentMask))
                {
                    MatchingArchetypesBuffer.Add(i, archetypes[i]);
                }
            }
            world.worldArchetypesRWLock.ExitReadLock();
        }

        
        public void Update(Archetype archetype)
        {
            if (Matches(archetype.ComponentMask))
            {
                MatchingArchetypesBuffer.Add(archetype.Index, archetype);
            }
        }

        public void Remove(Archetype archetype)
        {
            MatchingArchetypesBuffer.RemoveAt(archetype.Index);
        }

        
        public bool Matches(BitMask mask)
        {
            bool candidate = componentMask.HasMask.AllMatch(mask) && !componentMask.ExcludeMask.AnyMatch(mask);
            if (candidate)
            {
                for (int i = 0; i < componentMask.SomeOfMasks.Length; i++)
                {
                    candidate &= componentMask.SomeOfMasks[i].AnyMatch(mask);
                }
                for (int i = 0; i < componentMask.NotAllMasks.Length; i++)
                {
                    candidate &= !componentMask.NotAllMasks[i].AllMatch(mask);
                }
            }
            return candidate;
        }

        
        public EntityEnumerator GetEnumerator()
        {
            return new EntityEnumerator(MatchingArchetypes);
        }

#pragma warning disable CA1034 // Nested types should not be visible
        public ref struct ArchetypeEnumerator
#pragma warning restore CA1034 // Nested types should not be visible
        {
            ReadOnlySpan<Archetype> buffer;
            int currentArchetypeIndex;
            
            public ArchetypeEnumerator(ReadOnlySpan<Archetype> buffer)
            {
                this.buffer = buffer;
                currentArchetypeIndex = 0;
            }

            public Archetype Current
            {
                
                get
                {
                    return buffer[currentArchetypeIndex];
                }
            }

            
            public bool MoveNext()
            {
                return ++currentArchetypeIndex < buffer.Length;
            }

            
            public void Reset()
            {
                currentArchetypeIndex = 0;
            }
        }
#pragma warning disable CA1034 // Nested types should not be visible
        public ref struct EntityEnumerator
#pragma warning restore CA1034 // Nested types should not be visible
        {
            ReadOnlySpan<Archetype> buffer;
            int currentArchetypeIndex;
            int currentCount;
            int currentEntity;
            
            public EntityEnumerator(ReadOnlySpan<Archetype> buffer)
            {
                this.buffer = buffer;
                currentArchetypeIndex = 0;
                currentEntity = -1;
                currentCount = buffer.Length > 0 ? buffer[0].elementCount : 0;
            }

            public Entity Current
            {
                
                get
                {
                    return buffer[currentArchetypeIndex].Entities[currentEntity];
                }
            }

            
            public bool MoveNext()
            {
                currentEntity++;
                if (currentEntity >= currentCount)
                {
                    bool hasNext;
                    do
                    {
                        hasNext = ++currentArchetypeIndex < buffer.Length;
                        if (hasNext)
                        {
                            currentCount = buffer[currentArchetypeIndex].elementCount;
                            currentEntity = 0;
                        }
                        else
                        {
                            currentEntity = -1;
                        }
                    } while (hasNext && currentCount <= 0);
                    return hasNext;
                }
                return true;
            }

            
            public void Reset()
            {
                currentEntity = -1;
                currentArchetypeIndex = 0;
                currentCount = buffer.Length > 0 ? buffer[0].elementCount : 0;
            }
        }

        public void Dispose()
        {
            MatchingArchetypesBuffer.Dispose();
        }
    }
}

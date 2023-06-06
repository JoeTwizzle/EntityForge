using EntityForge.Collections;
using EntityForge.Collections.Generic;
using System.Runtime.CompilerServices;

namespace EntityForge.Queries
{
    public sealed class EntityFilter : IDisposable
    {
        internal readonly World world;
        internal readonly BitMask hasMask;
        internal readonly BitMask excMask;
        internal readonly BitMask[] someMasks;

        public ReadOnlySpan<Archetype> MatchingArchetypes
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return MatchingArchetypesBuffer.GetDenseData();
            }
        }

        UnsafeSparseSet<Archetype> MatchingArchetypesBuffer;
        public int MatchCount => MatchingArchetypesBuffer.DenseCount;

        internal EntityFilter(World world, BitMask hasMask, BitMask excMask, BitMask[] someMasks)
        {
            this.world = world;
            MatchingArchetypesBuffer = new();
            this.excMask = excMask;
            this.hasMask = hasMask;
            this.someMasks = someMasks;
            world.worldArchetypesRWLock.EnterReadLock();
            for (int i = 0; i < world.ArchtypeCount; i++)
            {
                if (Matches(world.AllArchetypes[i].ComponentMask))
                {
                    MatchingArchetypesBuffer.Add(i, world.AllArchetypes[i]);
                }
            }
            world.worldArchetypesRWLock.ExitReadLock();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Matches(BitMask mask)
        {
            bool someMatches = true;
            for (int i = 0; i < someMasks.Length; i++)
            {
                someMatches &= someMasks[i].AnyMatch(mask);
            }

            return someMatches && hasMask.AllMatch(mask) && !excMask.AnyMatch(mask);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ArchetypeEnumerator(ReadOnlySpan<Archetype> buffer)
            {
                this.buffer = buffer;
                currentArchetypeIndex = 0;
            }

            public Archetype Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    return buffer[currentArchetypeIndex];
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                return ++currentArchetypeIndex < buffer.Length;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public EntityEnumerator(ReadOnlySpan<Archetype> buffer)
            {
                this.buffer = buffer;
                currentArchetypeIndex = 0;
                currentEntity = -1;
                currentCount = buffer.Length > 0 ? buffer[0].ElementCount : 0;
            }

            public Entity Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    return buffer[currentArchetypeIndex].Entities[currentEntity];
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
                            currentCount = buffer[currentArchetypeIndex].ElementCount;
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

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Reset()
            {
                currentEntity = -1;
                currentArchetypeIndex = 0;
                currentCount = buffer.Length > 0 ? buffer[0].ElementCount : 0;
            }
        }

        public void Dispose()
        {
            MatchingArchetypesBuffer.Dispose();
        }
    }
}

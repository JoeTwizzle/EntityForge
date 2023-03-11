using Archie.Helpers;
using System.Buffers;
using System.Runtime.CompilerServices;

namespace Archie.Queries
{
    public sealed class EntityFilter
    {
        internal readonly World world;
        internal readonly BitMask incMask;
        internal readonly BitMask excMask;


        public Span<Archetype> MatchingArchetypes
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new Span<Archetype>(MatchingArchetypesBuffer, 0, MatchCount);
            }
        }

        internal Dictionary<Archetype, int> MatchingArchetypesMap;
        internal Archetype[] MatchingArchetypesBuffer;
        public int MatchCount;

        internal EntityFilter(World world, BitMask incMask, BitMask excMask)
        {
            this.world = world;
            MatchingArchetypesMap = new();
            this.excMask = excMask;
            this.incMask = incMask;
            MatchingArchetypesBuffer = ArrayPool<Archetype>.Shared.Rent(5);
            for (int i = 0; i < world.ArchtypeCount; i++)
            {
                if (Matches(world.AllArchetypes[i].BitMask))
                {
                    MatchingArchetypesBuffer = MatchingArchetypesBuffer.GrowIfNeededPooled(MatchCount, 1, true);
                    MatchingArchetypesBuffer[MatchCount++] = world.AllArchetypes[i];
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Update(Archetype archetype)
        {
            if (Matches(archetype.BitMask))
            {
                MatchingArchetypesMap.Add(archetype, MatchCount);
                MatchingArchetypesBuffer = MatchingArchetypesBuffer.GrowIfNeededPooled(MatchCount, 1, true);
                MatchingArchetypesBuffer[MatchCount++] = archetype;
            }
        }

        public void Remove(Archetype archetype)
        {
            if (MatchingArchetypesMap.TryGetValue(archetype, out var index))
            {
                MatchingArchetypesBuffer[index] = MatchingArchetypesBuffer[--MatchCount];
                MatchingArchetypesMap.Remove(archetype);
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
                currentEntity = 0;
                currentCount = buffer.Length > 0 ? buffer[0].InternalEntityCount : 0;
            }

            public EntityId Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    return new EntityId(currentEntity);
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                if (currentEntity >= currentCount)
                {
                    bool hasNext = ++currentArchetypeIndex < buffer.Length;
                    if (hasNext)
                    {
                        currentCount = buffer[currentArchetypeIndex].InternalEntityCount;
                    }
                    return hasNext;
                }
                ++currentEntity;
                return true;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Reset()
            {
                currentArchetypeIndex = 0;
            }
        }
    }
}

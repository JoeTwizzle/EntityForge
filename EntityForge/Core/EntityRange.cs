namespace EntityForge.Core
{
    public struct EntityRange
    {
        public readonly short WorldId;
        public readonly int Start;
        public readonly int Count;

        internal EntityRange(short worldId, int start, int count)
        {
            WorldId = worldId;
            Start = start;
            Count = count;
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(WorldId, Start, Start + Count);
        }

        public struct Enumerator
        {
            public readonly short WorldId;
            public readonly int First;
            public readonly int End;

            public int currentIndex;

            internal Enumerator(short worldId, int first, int end)
            {
                WorldId = worldId;
                First = first;
                End = end;

                currentIndex = First - 1;
            }

            public Entity Current => new Entity(currentIndex, 1, WorldId);

            public bool MoveNext()
            {
                return ++currentIndex < End;
            }

            public void Reset()
            {
                currentIndex = First - 1;
            }
        }
    }
}

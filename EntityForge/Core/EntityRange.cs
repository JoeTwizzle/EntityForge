using System.Diagnostics;

namespace EntityForge
{
    public struct EntityRange : IEquatable<EntityRange>
    {
        public readonly int Start;
        public readonly int Count;

        internal EntityRange(int start, int count)
        {
            Start = start;
            Count = count;
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(Start, Start + Count);
        }

        public EntityId GetEntityId(int i)
        {
            if (i < Start || i >= Count) throw new ArgumentOutOfRangeException(nameof(i));
            return new EntityId(Start + i);
        }

#pragma warning disable CA1034 // Nested types should not be visible
        public struct Enumerator : IEquatable<Enumerator>
#pragma warning restore CA1034 // Nested types should not be visible
        {
            private readonly int _start;
            private readonly int _end;
            private int _current;

            public Enumerator(int start, int count)
            {
                _current = start - 1;
                _start = start;
                _end = start + count;
            }

            public bool MoveNext()
            {
                return (++_current < _end);
            }

            public bool Equals(Enumerator other)
            {
                return _start == other._start && _end == other._end;
            }

            public override bool Equals(object? obj)
            {
                return obj is Enumerator && Equals((Enumerator)obj);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(_start, _end, _current);
            }
            public static bool operator ==(Enumerator left, Enumerator right)
            {
                return left.Equals(right);
            }

            public static bool operator !=(Enumerator left, Enumerator right)
            {
                return !(left == right);
            }
        }

        public override bool Equals(object? obj)
        {
            return obj is Entity entity && Equals((Entity)obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Start, Count);
        }

        public static bool operator ==(EntityRange left, EntityRange right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(EntityRange left, EntityRange right)
        {
            return !(left == right);
        }

        public bool Equals(EntityRange other)
        {
            return Start == other.Start && Count == other.Count;
        }
    }
}

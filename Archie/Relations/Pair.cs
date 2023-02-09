namespace Archie.Relations
{
    public struct Pair<TKey, TValue> : IEquatable<Pair<TKey, TValue>> where TKey : struct, IComponent<TKey> where TValue : struct, IComponent<TValue>
    {
        public TKey Key;
        public TValue Value;

        public override bool Equals(object? obj)
        {
            return obj is Pair<TKey, TValue> p && Equals(p);
        }

        public override int GetHashCode()
        {
            int hash = 17;
            hash = hash * 486187739 + Key.GetHashCode();
            hash = hash * 486187739 + Value.GetHashCode();
            return hash;
        }

        public static bool operator ==(Pair<TKey, TValue> left, Pair<TKey, TValue> right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Pair<TKey, TValue> left, Pair<TKey, TValue> right)
        {
            return !(left == right);
        }

        public bool Equals(Pair<TKey, TValue> other)
        {
            return other.Key.Equals(Key) && other.Value.Equals(Value);
        }
    }
}

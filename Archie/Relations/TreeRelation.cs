using Archie.Helpers;
using System.Buffers;
using System.Collections.Generic;

namespace Archie.Relations
{
    public struct TreeRelation : IEquatable<TreeRelation>
    {
        public EntityId? Parent => parentInternal;
        internal EntityId? parentInternal;
        private Dictionary<EntityId, int> childrenIndices;
        private EntityId[] children;
        private int childCount;
        public ReadOnlySpan<EntityId> Children => new Span<EntityId>(children, 0, childCount);

        internal void Dispose()
        {
            ArrayPool<EntityId>.Shared.Return(children);
        }

        internal void AddChild(EntityId child)
        {
            if (children == null)
            {
                children = ArrayPool<EntityId>.Shared.Rent(1);
                childrenIndices = new();
            }
            children.GrowIfNeededPooled(childCount, 1);
            children[childCount] = child;
            childrenIndices.Add(child, childCount++);
        }

        internal void RemoveChild(EntityId child)
        {
            if (childCount > 1)
            {
                int index = childrenIndices[child];
                children[index] = children[--childCount];
            }
            childrenIndices.Remove(child);
        }

        public bool HasChild(EntityId child)
        {
            return childrenIndices.ContainsKey(child);
        }

        public override bool Equals(object? obj)
        {
            return obj is TreeRelation t && Equals(t);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 486187739 + parentInternal.GetHashCode();
                var c = Children;
                for (int i = 0; i < c.Length; i++)
                {
                    hash = hash * 486187739 + c[i].GetHashCode();
                }
                return hash;
            }
        }

        public static bool operator ==(TreeRelation left, TreeRelation right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(TreeRelation left, TreeRelation right)
        {
            return !(left == right);
        }

        public bool Equals(TreeRelation other)
        {
            return parentInternal == other.parentInternal && Children.SequenceEqual(other.Children);
        }
    }
}

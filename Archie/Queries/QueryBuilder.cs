using Archie.Relations;
using System.Runtime.InteropServices;

namespace Archie.Queries
{
    struct RelationData
    {
        public int Src;
        public int Dst;
        public Action<Array, int, EntityId, BitMask>? Filter;

        public RelationData(int src, int dst, Action<Array, int, EntityId, BitMask>? filter)
        {
            Src = src;
            Dst = dst;
            Filter = filter;
        }
    }
    internal class QueryBuilder
    {
        const int SelfId = 0;
        internal int idCount;
        internal Dictionary<string, int> labelIds;
        internal List<ComponentMaskBuilder> bitMasks;
        internal List<RelationData> relations;

        public QueryBuilder(string selfIdentifier = "this")
        {
            idCount = 1;
            labelIds = new Dictionary<string, int>()
            {
                { selfIdentifier, SelfId },
            };
            relations = new List<RelationData>();
            bitMasks = new List<ComponentMaskBuilder>();
        }

        public QueryBuilder WithParent<T>(string dest) where T : struct, IComponent<T>, ITreeRelation<T>
        {
            ref var destVal = ref CollectionsMarshal.GetValueRefOrAddDefault(labelIds, dest, out var exist);
            if (!exist)
            {
                destVal = idCount++;
                bitMasks.Add(ComponentMaskBuilder.Create());
            }
            relations.Add(new RelationData(0, destVal, (array, count, target, mask) =>
            {
                var s = new Span<T>(((T[])array), 0, count);
                for (int i = 0; i < s.Length; i++)
                {
                    if (mask.IsSet(i))
                    {
                        if (!s[i].GetRelation().HasChild(target))
                        {
                            mask.ClearBit(i);
                        }
                    }
                }
            }));
            return this;
        }

        public QueryBuilder Inc<T>(string target) where T : struct, IComponent<T>
        {
            var id = labelIds[target];
            var span = CollectionsMarshal.AsSpan(bitMasks);
            span[id] = span[id].Inc<T>();
            return this;
        }

        public QueryBuilder Exc<T>(string target) where T : struct, IComponent<T>
        {
            var id = labelIds[target];
            var span = CollectionsMarshal.AsSpan(bitMasks);
            span[id] = span[id].Exc<T>();
            return this;
        }

        public Query End()
        {
            return new Query(this);
        }
    }
}

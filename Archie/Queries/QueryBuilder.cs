using Archie.Relations;
using System.Runtime.InteropServices;

namespace Archie.Queries
{
    readonly struct RelationData
    {
        public readonly int Source;
        public readonly int Destination;
        public readonly RelationKind RelationKind;
        public readonly Type RelationType;

        public RelationData(int source, int destination, RelationKind relationKind, Type relationType)
        {
            Source = source;
            Destination = destination;
            RelationKind = relationKind;
            RelationType = relationType;
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

        public QueryBuilder Rel<T>(string dest) where T : struct, IComponent<T>, IRelation<T>
        {
            ref var destVal = ref CollectionsMarshal.GetValueRefOrAddDefault(labelIds, dest, out var exist);
            if (!exist)
            {
                destVal = idCount++;
                bitMasks.Add(ComponentMaskBuilder.Create());
            }
            relations.Add(new RelationData(SelfId, destVal, T.RelationKind, typeof(T)));
            return this;
        }

        public QueryBuilder Rel<T>(string source, string dest) where T : struct, IComponent<T>, IRelation<T>
        {
            ref var destVal = ref CollectionsMarshal.GetValueRefOrAddDefault(labelIds, dest, out var exist);
            if (!exist)
            {
                destVal = idCount++;
                bitMasks.Add(ComponentMaskBuilder.Create());
            }

            relations.Add(new RelationData(labelIds[source], destVal, T.RelationKind, typeof(T)));
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

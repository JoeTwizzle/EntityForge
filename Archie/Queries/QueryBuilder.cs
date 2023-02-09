using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Archie.Queries
{
    internal class QueryBuilder
    {
        internal int idCount;
        internal Dictionary<string, int> labelIds;
        internal List<ComponentMaskBuilder> bitMasks;
        internal List<(int, int, Type)> relations;

        public QueryBuilder(string defaultKey = "this")
        {
            idCount = 1;
            labelIds = new Dictionary<string, int>()
            {
                { defaultKey, 0}
            };
            relations = new List<(int, int, Type)>();
            bitMasks = new List<ComponentMaskBuilder>();
        }

        public QueryBuilder Rel<T>(string target, string dest) where T : struct, IComponent<T>
        {
            ref var destVal = ref CollectionsMarshal.GetValueRefOrAddDefault(labelIds, dest, out var exist);
            if (!exist)
            {
                destVal = idCount++;
                bitMasks.Add(ComponentMaskBuilder.Create());
            }
            relations.Add((labelIds[target], destVal, typeof(T)));
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

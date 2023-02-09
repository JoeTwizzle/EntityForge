using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Archie.Queries
{
    internal class Query
    {
        internal ComponentMask[] bitMasks;
        internal (int, int, Type)[] relations;

        public Query(QueryBuilder builder)
        {
            bitMasks = new ComponentMask[builder.bitMasks.Count];
            for (int i = 0; i < builder.bitMasks.Count; i++)
            {
                bitMasks[i] = builder.bitMasks[i].End();
            }
            relations = builder.relations.ToArray();
        }
    }
}

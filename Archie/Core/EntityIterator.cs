using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Archie.Queries;

namespace Archie.Core
{
    internal class EntityIterator
    {
        EntityFilter[] matchingFilters;

        public EntityIterator(World world, Query query)
        {
            matchingFilters = new EntityFilter[query.bitMasks.Length];
            for (int i = 0; i < matchingFilters.Length; i++)
            {
                world.GetFilter(query.bitMasks[i]);
            }

        }
    }
}

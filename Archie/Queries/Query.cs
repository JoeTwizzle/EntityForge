namespace Archie.Queries
{
    internal class Query
    {
        internal EntityFilter[] filters;
        internal RelationData[] relations;
        World world;
        public Query(QueryBuilder builder)
        {
            filters = new EntityFilter[builder.bitMasks.Count];
            for (int i = 0; i < builder.bitMasks.Count; i++)
            {
                filters[i] = world.GetFilter(builder.bitMasks[i].End());
            }
            relations = builder.relations.ToArray();
        }

        void Update(Archetype archetype)
        {
            for (int i = 0; i < relations.Length; i++)
            {
                var filter = filters[relations[i].Source];
                filter.Update(archetype);
            }
        }

        void Test()
        {
            for (int i = 0; i < relations.Length; i++)
            {
                var archetypes = filters[relations[i].Source].MatchingArchetypes;

            }

            for (int i = 0; i < world.EntityIndices.Length; i++)
            {
                var idx = world.EntityIndices[i];

            }
        }
    }
}

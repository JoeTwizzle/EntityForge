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
                var filter = filters[relations[i].Src];
                filter.Update(archetype);
            }
        }

        void Test()
        {
            for (int i = 0; i < relations.Length; i++)
            {
                var archetypesFrom = filters[relations[i].Src].MatchingArchetypes;

                var archetypesTo = filters[relations[i].Dst].MatchingArchetypes;

                if (!relations[i])
                {
                    for (int j = 0; j < archetypesFrom.Length; j++)
                    {
                        var p = archetypesFrom[j].DangerousGetPool(new ComponentId(relations[i].Id, 0, null!));
                        for (int entIndex = 0; entIndex < p.Length; entIndex++)
                        {
                            
                        }
                    }
                }
                else
                {

                }
            }

            for (int i = 0; i < world.EntityIndices.Length; i++)
            {
                var idx = world.EntityIndices[i];

            }
        }
    }
}

using EntityForge.Collections;
using EntityForge.Tags;
using System.Runtime.CompilerServices;

namespace EntityForge.Queries
{
    public sealed class EntityFilter
    {
        public readonly ArchetypeFilter ArchetypeFilter;
        public readonly TagMask TagMask;

        internal readonly BitMask _filterMask;

        public EntityFilter(ArchetypeFilter archetypeFilter, TagMask tagMask)
        {
            ArchetypeFilter = archetypeFilter;
            TagMask = tagMask;
            _filterMask = new();
        }

        public EntityFilter(ArchetypeFilter archetypeFilter, TagMask tagMask, BitMask filterMask)
        {
            ArchetypeFilter = archetypeFilter;
            TagMask = tagMask;
            _filterMask = filterMask;
        }

        public ReadOnlySpan<long> GetMatches(Archetype archetype)
        {
            TagMask.Match(archetype, _filterMask);
            return _filterMask.Bits;
        }

        public EntityEnumerator GetEnumerator()
        {
            return new EntityEnumerator(this);
        }

#pragma warning disable CA1034 // Nested types should not be visible
        public ref struct EntityEnumerator
#pragma warning restore CA1034 // Nested types should not be visible
        {
            ReadOnlySpan<Archetype> archetypes;
            Span<TagBearer> tags;
            int currentArchetypeIndex;
            int currentEntity;
            EntityFilter filter;
            bool requiresTags;

            public EntityEnumerator(EntityFilter filter)
            {
                this.filter = filter;
                this.archetypes = filter.ArchetypeFilter.MatchingArchetypes;
                bool candidate = filter.TagMask.HasTags.IsAllZeros();
                for (int i = 0; i < filter.TagMask.SomeTags.Length; i++)
                {
                    candidate &= filter.TagMask.SomeTags[i].IsAllZeros();
                }
                requiresTags = !candidate;
                currentArchetypeIndex = 0;
                currentEntity = -1;
            }

            public Entity Current
            {
                
                get
                {
                    return archetypes[currentArchetypeIndex].Entities[currentEntity];
                }
            }

            public Archetype CurrentArchetype
            {
                get
                {
                    return archetypes[currentArchetypeIndex];
                }
            }

            
            public bool MoveNext()
            {
                bool entityInvalid = true;
                do
                {
                    if (++currentEntity >= CurrentArchetype.ElementCount)
                    {
                        bool archetypeInvalid = true;
                        do
                        {
                            currentEntity = 0;
                            if (++currentArchetypeIndex >= CurrentArchetype.ElementCount)
                            {
                                return false;
                            }
                            archetypeInvalid = !ArchetypeMatchesTags();
                        }
                        while (archetypeInvalid);
                    }
                    //check if matches tags
                    entityInvalid = !EntityMatchesTags();

                } while (entityInvalid);

                return true;
            }

            
            bool ArchetypeMatchesTags()
            {
                if (requiresTags)
                {
                    if (CurrentArchetype.TryGetComponentIndex<TagBearer>(out int index))
                    {
                        tags = CurrentArchetype.GetPool<TagBearer>(index);
                        return true;
                    }
                    return false;
                }
                return true;
            }

            
            bool EntityMatchesTags()
            {
                if (requiresTags)
                {
                    var tag = tags[currentEntity];
                    bool candidate = filter.TagMask.HasTags.AllMatch(tags[currentEntity].mask) && !filter.TagMask.NoTags.AnyMatch(tag.mask);
                    if (!candidate) //does not have the required tags set
                    {
                        return false;
                    }
                    for (int j = 0; j < filter.TagMask.SomeTags.Length && candidate; j++)
                    {
                        candidate &= filter.TagMask.SomeTags[j].AnyMatch(tag.mask);
                    }
                    for (int j = 0; j < filter.TagMask.NotAllTags.Length && candidate; j++)
                    {
                        candidate &= !filter.TagMask.NotAllTags[j].AllMatch(tag.mask);
                    }

                    return candidate;
                }
                return true;
            }
        }
    }
}

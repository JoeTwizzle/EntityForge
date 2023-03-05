using System.Diagnostics.CodeAnalysis;

namespace Archie
{
    public struct ArchetypeBuilder
    {
        List<ComponentId> types;
        //Dictionary<(Type, int), int> idMap;
        //int counter;
        public static ArchetypeBuilder Create()
        {
            return new ArchetypeBuilder();
        }

        public ArchetypeBuilder()
        {
            types = new();
            //idMap = new();
        }

        [UnscopedRefAttribute]
        public ref ArchetypeBuilder Inc<T>(int variant = World.DefaultVariant) where T : struct, IComponent<T>
        {
            types.Add(new ComponentId(World.GetOrCreateTypeId<T>(), variant, typeof(T)));
            return ref this;
        }

        //[UnscopedRefAttribute]
        //public ref ArchetypeBuilder TreeRelation<T>() where T : struct, ITreeRelation<T>, IComponent<T>
        //{
        //    switch (T.RelationKind)
        //    {
        //        case RelationKind.Discriminated:
        //            ThrowHelper.ThrowArgumentException("Discriminated relations require identifying targetInternal entities");
        //            break;
        //        case RelationKind.SingleSingle:
        //            types.Add(new ComponentId(World.GetOrCreateTypeId<OneToOneRelation<T>>(), World.DefaultVariant, typeof(OneToOneRelation<T>)));
        //            break;
        //        case RelationKind.SingleMulti:
        //            types.Add(new ComponentId(World.GetOrCreateTypeId<OneToManyRelation<T>>(), World.DefaultVariant, typeof(OneToManyRelation<T>)));
        //            break;
        //        case RelationKind.MultiMulti:
        //            types.Add(new ComponentId(World.GetOrCreateTypeId<ManyToManyRelation<T>>(), World.DefaultVariant, typeof(ManyToManyRelation<T>)));
        //            break;
        //    }
        //    return ref this;
        //}

        //[UnscopedRefAttribute]
        //public ref ArchetypeBuilder TreeRelation<T>(EntityId entity) where T : struct, ITreeRelation<T>, IComponent<T>
        //{
        //    if (T.RelationKind != RelationKind.Discriminated)
        //    {
        //        ThrowHelper.ThrowArgumentException("Non-discriminated relations can't have identifying targetInternal entities");
        //    }
        //    types.Add(new ComponentId(World.GetOrCreateTypeId<DiscriminatingOneToOneRelation<T>>(), entity.Id, typeof(DiscriminatingOneToOneRelation<T>)));
        //    return ref this;
        //}


        public ArchetypeDefinition End()
        {
            var components = types.ToArray();
            World.SortTypes(components);
            components = World.RemoveDuplicates(components);
            return new ArchetypeDefinition(World.GetComponentHash(components), components);
        }
    }
}

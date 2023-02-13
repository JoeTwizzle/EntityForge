using Archie.Helpers;
using Archie.Relations;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Archie
{
    public struct ArchetypeBuilder
    {
        List<(Type, int)> types;
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
        public ref ArchetypeBuilder Inc<T>() where T : struct, IComponent<T>
        {
            types.Add((typeof(T), World.DefaultVariant));
            return ref this;
        }

        [UnscopedRefAttribute]
        public ref ArchetypeBuilder Relation<T>() where T : struct, IRelation<T>, IComponent<T>
        {
            switch (T.RelationKind)
            {
                case RelationKind.SingleSingleDiscriminated:
                    ThrowHelper.ThrowArgumentException("Discriminated relations require identifying target entities");
                    break;
                case RelationKind.SingleSingle:
                    types.Add((typeof(OneToOneRelation<T>), World.DefaultVariant));
                    break;
                case RelationKind.SingleMulti:
                    types.Add((typeof(OneToManyRelation<T>), World.DefaultVariant));
                    break;
                case RelationKind.MultiMulti:
                    types.Add((typeof(ManyToManyRelation<T>), World.DefaultVariant));
                    break;
            }
            return ref this;
        }

        [UnscopedRefAttribute]
        public ref ArchetypeBuilder Relation<T>(EntityId entity) where T : struct, IRelation<T>, IComponent<T>
        {
            if (T.RelationKind != RelationKind.SingleSingleDiscriminated)
            {
                ThrowHelper.ThrowArgumentException("Non-discriminated relations can't have identifying target entities");
            }
            types.Add((typeof(DiscriminatingOneToOneRelation<T>), entity.Id));
            return ref this;
        }


        public ArchetypeDefinition End()
        {
            var components = types.ToArray();
            World.SortTypes(components);
            components = World.RemoveDuplicates(components);
            return new ArchetypeDefinition(World.GetComponentHash(components), components);
        }
    }
}

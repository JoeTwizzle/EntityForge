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
        List<Type> types;
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
            types.Add(typeof(T));
            return ref this;
        }

        [UnscopedRefAttribute]
        public ref ArchetypeBuilder Relation<T>() where T : struct, IRelation<T>, IComponent<T>
        {
            switch (T.RelationKind)
            {
                //case RelationKind.SingleSingleDiscriminated:
                //    throw new ArgumentException("Discriminated relations require identifying target entities");
                case RelationKind.SingleSingle:
                    types.Add(typeof(OneToOneRelation<T>));
                    break;
                case RelationKind.SingleMulti:
                    types.Add(typeof(OneToManyRelation<T>));
                    break;
                case RelationKind.MultiMulti:
                    types.Add(typeof(ManyToManyRelation<T>));
                    break;
                default:
                    throw new ArgumentException("Illegal enum value");
            }

            return ref this;
        }

        //[UnscopedRefAttribute]
        //public ref ArchetypeBuilder Relation<T>(EntityId entity) where T : struct, IRelation<T>, IComponent<T>
        //{
        //    var key = (typeof(T), entity.Id);
        //    ref var val = ref CollectionsMarshal.GetValueRefOrAddDefault(idMap, key, out var exists);
        //    if (!exists)
        //    {
        //        val = counter++;
        //    }

        //    switch (T.RelationKind)
        //    {
        //        case RelationKind.SingleSingleDiscriminated:
        //            idMap.Add(key, val);
        //            types.Add(typeof(DiscriminatingOneToOneRelation<T>));
        //            break;
        //        default:
        //            throw new ArgumentException("Non-discriminated relations can't have identifying target entities");
        //    }

        //    return ref this;
        //}


        public ArchetypeDefinition End()
        {
            var components = types.ToArray();
            World.SortTypes(components);
            components = World.RemoveDuplicates(components);
            return new ArchetypeDefinition(World.GetComponentHash(components), components, Array.Empty<(Type, int)>());
        }
    }
}

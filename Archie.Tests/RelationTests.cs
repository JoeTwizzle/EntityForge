using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Archie.Tests
{
    internal class RelationTests
    {
        ArchetypeDefinition def = ArchetypeBuilder.Create().Relation<Rel1>().Inc<Component1>().Inc<Component2>().Inc<Component3>().End();

        World world;

        [SetUp]
        public void Init()
        {
            world = new World();

        }


        [Test]
        public void Test()
        {
            var ent = world.CreateEntityImmediate();
            var ent2 = world.CreateEntityImmediate();
            world.AddRelationTarget(ent, ent2, new Rel1(5));


        }
    }
}

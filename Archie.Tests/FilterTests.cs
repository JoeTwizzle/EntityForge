using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Archie.Tests
{
    public class FilterTests
    {
        ArchetypeDefinition archetypeC0 = Archetype.CreateDefinition(new Type[] { });
        ArchetypeDefinition archetypeC1 = Archetype.CreateDefinition(new Type[] { typeof(Component1) });
        ArchetypeDefinition archetypeC2 = Archetype.CreateDefinition(new Type[] { typeof(Component2) });
        ArchetypeDefinition archetypeC3 = Archetype.CreateDefinition(new Type[] { typeof(Component3) });
        ArchetypeDefinition archetypeC1C2 = Archetype.CreateDefinition(new Type[] { typeof(Component1), typeof(Component2) });
        ArchetypeDefinition archetypeC1C3 = Archetype.CreateDefinition(new Type[] { typeof(Component1), typeof(Component3) });
        ArchetypeDefinition archetypeC2C3 = Archetype.CreateDefinition(new Type[] { typeof(Component2), typeof(Component3) });
        ArchetypeDefinition archetypeC1C2C3 = Archetype.CreateDefinition(new Type[] { typeof(Component1), typeof(Component2), typeof(Component3) });


        World world;
        [SetUp]
        public void Setup()
        {
            world = new World();
            world.CreateEntityImmediate(archetypeC0);
            world.CreateEntityImmediate(archetypeC1);
            world.CreateEntityImmediate(archetypeC2);
            world.CreateEntityImmediate(archetypeC3);
            world.CreateEntityImmediate(archetypeC1C2);
            world.CreateEntityImmediate(archetypeC1C3);
            world.CreateEntityImmediate(archetypeC2C3);
            world.CreateEntityImmediate(archetypeC1C2C3);
        }


        [Test]
        public void FilterIncSingleTest()
        {
            var filter = world.FilterInc<Component1>().End();
            Assert.AreEqual(4, filter.ArchetypeCount);
        }

        [Test]
        public void FilterIncTwoTest()
        {
            var filter = world.FilterInc<Component1>().Inc<Component2>().End();
            Assert.AreEqual(2, filter.ArchetypeCount);
        }

        [Test]
        public void FilterIncThreeTest()
        {
            var filter = world.FilterInc<Component1>().Inc<Component2>().Inc<Component3>().End();
            Assert.AreEqual(1, filter.ArchetypeCount);
        }

        [Test]
        public void FilterIterateTest()
        {
            var filter = world.FilterInc<Component1>().End();
            foreach (var entity in filter)
            {
                Assert.True(world.HasComponent<Component1>(entity));
            }
        }
    }
}

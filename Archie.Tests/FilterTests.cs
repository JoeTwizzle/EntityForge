using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Archie.Tests
{
    public partial class FilterTests
    {
        ArchetypeDefinition archetypeC0 = Archetype.CreateDefinition(new Type[] { });
        ArchetypeDefinition archetypeC1 = Archetype.CreateDefinition(new Type[] { typeof(Component1) });
        ArchetypeDefinition archetypeC2 = Archetype.CreateDefinition(new Type[] { typeof(Component2) });
        ArchetypeDefinition archetypeC3 = Archetype.CreateDefinition(new Type[] { typeof(Component3) });
        ArchetypeDefinition archetypeC1C2 = Archetype.CreateDefinition(new Type[] { typeof(Component1), typeof(Component2) });
        ArchetypeDefinition archetypeC1C3 = Archetype.CreateDefinition(new Type[] { typeof(Component1), typeof(Component3) });
        ArchetypeDefinition archetypeC2C3 = Archetype.CreateDefinition(new Type[] { typeof(Component2), typeof(Component3) });
        ArchetypeDefinition archetypeC1C2C3 = Archetype.CreateDefinition(new Type[] { typeof(Component1), typeof(Component2), typeof(Component3) });
        ComponentMask mask1 = ComponentMask.Create().Inc<Component1>().End();
        ComponentMask mask1x2 = ComponentMask.Create().Inc<Component1>().Exc<Component2>().End();
        ComponentMask mask2 = ComponentMask.Create().Inc<Component1>().Inc<Component2>().End();
        ComponentMask mask3 = ComponentMask.Create().Inc<Component1>().Inc<Component2>().Inc<Component3>().End();

        [Archie.InjectTypes]
        ref partial struct Group1
        {
            public ref Component1 c1;
        }
        [Archie.InjectTypes]
        ref partial struct Group2
        {
            public ref Component1 c1;
        }
        [Archie.InjectTypes]
        ref partial struct Group3
        {
            public ref Component1 c1;
        }

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
            var filter = world.GetFilter(mask1);
            int i = 0;
            foreach (var item in filter)
            {
                i++;
            }
            Assert.AreEqual(4, i);
        }


        [Test]
        public void GroupIncSingleTest()
        {
            int i = 0;
            foreach (var archetype in world.GetMatchingArchetypes(mask1))
            {
                foreach (var group in Group1.GetIterator(archetype))
                {
                    i++;
                    group.c1.Value++;
                }
            }
            Assert.AreEqual(4, i);
        }

        [Test]
        public void FilterIncExcSingleTest()
        {
            var filter = world.GetFilter(mask1x2);
            int i = 0;
            foreach (var item in filter)
            {
                i++;
            }
            Assert.AreEqual(2, i);
        }

        [Test]
        public void FilterIncTwoTest()
        {
            var filter = world.GetFilter(mask2);
            int i = 0;
            foreach (var item in filter)
            {
                i++;
            }
            Assert.AreEqual(2, i);
        }

        [Test]
        public void FilterIncThreeTest()
        {
            var filter = world.GetFilter(mask3);
            int i = 0;
            foreach (var item in filter)
            {
                i++;
            }
            Assert.AreEqual(1, i);
        }

        [Test]
        public void FilterIterateTest()
        {
            var filter = world.GetFilter(mask1);
            foreach (var entity in filter)
            {
                Assert.True(world.HasComponent<Component1>(entity));
            }
            foreach (var entity in filter)
            {
                Assert.True(world.HasComponent<Component1>(entity));
            }
        }
    }
}

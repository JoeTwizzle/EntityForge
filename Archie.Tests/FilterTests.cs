namespace Archie.Tests
{
    public partial class FilterTests
    {
        ArchetypeDefinition archetypeC0 = ArchetypeBuilder.Create().End();
        ArchetypeDefinition archetypeC1 = ArchetypeBuilder.Create().Inc<Component1>().End();
        ArchetypeDefinition archetypeC2 = ArchetypeBuilder.Create().Inc<Component2>().End();
        ArchetypeDefinition archetypeC3 = ArchetypeBuilder.Create().Inc<Component3>().End();
        ArchetypeDefinition archetypeC1C2 = ArchetypeBuilder.Create().Inc<Component1>().Inc<Component2>().End();
        ArchetypeDefinition archetypeC1C3 = ArchetypeBuilder.Create().Inc<Component1>().Inc<Component3>().End();
        ArchetypeDefinition archetypeC2C3 = ArchetypeBuilder.Create().Inc<Component2>().Inc<Component3>().End();
        ArchetypeDefinition archetypeC1C2C3 = ArchetypeBuilder.Create().Inc<Component1>().Inc<Component2>().Inc<Component3>().End();
        ComponentMask mask1 = ComponentMask.Create().Inc<Component1>().End();
        ComponentMask mask1x2 = ComponentMask.Create().Inc<Component1>().Exc<Component2>().End();
        ComponentMask mask2 = ComponentMask.Create().Inc<Component1>().Inc<Component2>().End();
        ComponentMask mask3 = ComponentMask.Create().Inc<Component1>().Inc<Component2>().Inc<Component3>().End();

        World world;
        [SetUp]
        public void Setup()
        {
            world = new World();
            world.CreateEntity(archetypeC0);
            world.CreateEntity(archetypeC1);
            world.CreateEntity(archetypeC2);
            world.CreateEntity(archetypeC3);
            world.CreateEntity(archetypeC1C2);
            world.CreateEntity(archetypeC1C3);
            world.CreateEntity(archetypeC2C3);
            world.CreateEntity(archetypeC1C2C3);
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

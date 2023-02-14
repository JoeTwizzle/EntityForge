
using Archie.Helpers;

namespace Archie.Tests
{
    public class WorldTests
    {
        ArchetypeDefinition archetypeC0 = ArchetypeBuilder.Create().End();
        ArchetypeDefinition archetypeC1 = ArchetypeBuilder.Create().Inc<Component1>().End();
        ArchetypeDefinition archetypeC2 = ArchetypeBuilder.Create().Inc<Component2>().End();
        ArchetypeDefinition archetypeC3 = ArchetypeBuilder.Create().Inc<Component3>().End();
        ArchetypeDefinition archetypeC1C2 = ArchetypeBuilder.Create().Inc<Component1>().Inc<Component2>().End();
        ArchetypeDefinition archetypeC1C3 = ArchetypeBuilder.Create().Inc<Component1>().Inc<Component3>().End();
        ArchetypeDefinition archetypeC2C3 = ArchetypeBuilder.Create().Inc<Component2>().Inc<Component3>().End();
        ArchetypeDefinition archetypeC1C2C3 = ArchetypeBuilder.Create().Inc<Component1>().Inc<Component2>().Inc<Component3>().End();
        World world;
        [SetUp]
        public void Setup()
        {
            world = new World();
        }

        [Test]
        public void AddRemoveComponentTest()
        {
            var entity = world.CreateEntityImmediate();
            Assert.AreEqual(world.GetArchetype(entity), world.GetArchetype(archetypeC0));
            world.AddComponentImmediate<ExampleComponent>(entity);
            Assert.AreEqual(world.GetArchetype(entity), world.GetArchetype(ArchetypeDefinition.Create().Inc<ExampleComponent>().End()));
            world.AddComponentImmediate<ExampleTransform>(entity);
            Assert.AreEqual(world.GetArchetype(entity), world.GetArchetype(ArchetypeDefinition.Create().Inc<ExampleComponent>().Inc<ExampleTransform>().End()));
            world.RemoveComponentImmediate<ExampleComponent>(entity);
            Assert.AreEqual(world.GetArchetype(entity), world.GetArchetype(ArchetypeDefinition.Create().Inc<ExampleTransform>().End()));
            world.RemoveComponentImmediate<ExampleTransform>(entity);
            Assert.AreEqual(world.GetArchetype(entity), world.GetArchetype(archetypeC0));
        }

        [Test]
        public void EntityTest()
        {
            var entity = world.Pack(world.CreateEntityImmediate());
            Assert.AreEqual(world.GetArchetype(entity.ToEntityId()), world.GetArchetype(archetypeC0));
            world.DestroyEntityImmediate(entity.ToEntityId());
            var e2 = world.Pack(world.CreateEntityImmediate());
            Assert.AreEqual(entity.Entity, e2.Entity);
            Assert.AreEqual(entity.World, e2.World);
            Assert.AreEqual(entity.Special, e2.Special);
            Assert.AreNotEqual(entity.Version, e2.Version);
        }

        [Test]
        public void CreateManyTest()
        {
            for (int i = 0; i < 10000; i++)
            {
                Assert.DoesNotThrow(() => world.CreateEntityImmediate(archetypeC1C2C3));
            }
        }

        [Test]
        public void EntityComponentTest()
        {
            var entity = world.CreateEntityImmediate();
            Assert.AreEqual(world.GetArchetype(entity), world.GetArchetype(archetypeC0));
            world.AddComponentImmediate<ExampleComponent>(entity);
            world.AddComponentImmediate<ExampleTransform>(entity);
#if DEBUG
            Assert.Throws<DuplicateComponentException>(() => world.AddComponentImmediate<ExampleTransform>(entity));
#else
            Assert.Throws<NullReferenceException>(() => world.AddComponentImmediate<ExampleTransform>(entity));
#endif
            world.DestroyEntityImmediate(entity);
        }

        [Test]
        public void NewDestroyNewTest()
        {
            var entity = world.Pack(world.CreateEntityImmediate());
            world.DestroyEntityImmediate(entity.ToEntityId());
            var e2 = world.Pack(world.CreateEntityImmediate());
            Assert.AreEqual(entity.Entity, e2.Entity);
            Assert.AreEqual(entity.World, e2.World);
            Assert.AreEqual(entity.Special, e2.Special);
            Assert.AreNotEqual(entity.Version, e2.Version);
        }

        [Test]
        public void NewAddDestroyAddTest()
        {
            var entity = world.CreateEntityImmediate();
            world.AddComponentImmediate<ExampleComponent>(entity);
            world.AddComponentImmediate<ExampleTransform>(entity);
            world.DestroyEntityImmediate(entity);
            var e2 = world.CreateEntityImmediate();
            Assert.DoesNotThrow(() => world.AddComponentImmediate<ExampleComponent>(e2));
            Assert.DoesNotThrow(() => world.AddComponentImmediate<ExampleTransform>(e2));
        }

        [Test]
        public void NewAddDestroyNewAddTest()
        {
            var entity = world.CreateEntityImmediate();
            world.AddComponentImmediate<ExampleComponent>(entity);
            world.AddComponentImmediate<ExampleTransform>(entity);
            world.DestroyEntityImmediate(entity);
            var e2 = world.CreateEntityImmediate();
#if DEBUG
            Assert.DoesNotThrow(() => world.AddComponentImmediate<ExampleComponent>(entity));
#else
            Assert.DoesNotThrow(() => world.AddComponentImmediate<ExampleComponent>(entity));
#endif
            Assert.DoesNotThrow(() => world.AddComponentImmediate<ExampleTransform>(e2));
        }

        [Test]
        public void NewAddRemoveAddTest()
        {
            var entity = world.CreateEntityImmediate();
            Assert.DoesNotThrow(() => world.AddComponentImmediate<ExampleComponent>(entity));
            Assert.DoesNotThrow(() => world.RemoveComponentImmediate<ExampleComponent>(entity));
            Assert.DoesNotThrow(() => world.AddComponentImmediate<ExampleComponent>(entity));
        }

        [Test]
        public void NewAddAddRemoveAddRemoveTest()
        {
            var entity = world.CreateEntityImmediate();
            Assert.DoesNotThrow(() => world.AddComponentImmediate<ExampleComponent>(entity));
            Assert.DoesNotThrow(() => world.AddComponentImmediate<ExampleTransform>(entity));
            Assert.DoesNotThrow(() => world.RemoveComponentImmediate<ExampleComponent>(entity));
            Assert.DoesNotThrow(() => world.AddComponentImmediate<ExampleComponent>(entity));
            Assert.DoesNotThrow(() => world.RemoveComponentImmediate<ExampleTransform>(entity));
        }

        [Test]
        public void MultiNewAddAddRemoveAddRemoveDestroyTest()
        {
            var entity = world.CreateEntityImmediate();
            Assert.DoesNotThrow(() => world.AddComponentImmediate<ExampleComponent>(entity));
            Assert.DoesNotThrow(() => world.AddComponentImmediate<ExampleTransform>(entity));
            Assert.DoesNotThrow(() => world.RemoveComponentImmediate<ExampleComponent>(entity));
            Assert.DoesNotThrow(() => world.AddComponentImmediate<ExampleComponent>(entity));
            Assert.DoesNotThrow(() => world.RemoveComponentImmediate<ExampleTransform>(entity));
            world.DestroyEntityImmediate(entity);
            entity = world.CreateEntityImmediate();
            Assert.DoesNotThrow(() => world.AddComponentImmediate<ExampleComponent>(entity));
            Assert.DoesNotThrow(() => world.AddComponentImmediate<ExampleTransform>(entity));
            Assert.DoesNotThrow(() => world.RemoveComponentImmediate<ExampleComponent>(entity));
            Assert.DoesNotThrow(() => world.AddComponentImmediate<ExampleComponent>(entity));
            Assert.DoesNotThrow(() => world.RemoveComponentImmediate<ExampleTransform>(entity));
        }

        EntityId[] InitMany(int count)
        {
            EntityId[] entites;
            entites = new EntityId[iterations];
            world = new World();
            world.ReserveEntities(archetypeC1C2, iterations);
            for (int i = 0; i < iterations; i++)
            {
                entites[i] = world.CreateEntityImmediate(archetypeC1C2);
            }
            return entites;
        }

        int iterations = 100000;
        [Test]
        public void AddManyTest()
        {
            var ents = InitMany(iterations);

            for (int i = 0; i < iterations; i++)
            {
                Assert.DoesNotThrow(() => world.AddComponentImmediate<Component3>(ents[i]));
            }
        }

        [Test]
        public void RemoveManyTest()
        {
            var ents = InitMany(iterations);

            for (int i = 0; i < ents.Length; i++)
            {
                Assert.DoesNotThrow(() => world.RemoveComponentImmediate<Component1>(ents[i]));
            }
        }

        [Test]
        public void RemoveManyTest2()
        {
            var ents = InitMany(iterations);

            for (int i = 0; i < ents.Length; i++)
            {
                Assert.DoesNotThrow(() => world.RemoveComponentImmediate<Component2>(ents[i]));
            }
        }
    }
}
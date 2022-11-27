using Archie.Helpers;

namespace Archie.Tests
{
    public class Tests
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
        }

        [Test]
        public void AddRemoveComponentTest()
        {
            var entity = world.CreateEntityImmediate();
            Assert.AreEqual(world.GetArchetype(entity), world.GetArchetype(Archetype.CreateDefinition(Array.Empty<Type>())));
            world.AddComponentImmediate<ExampleComponent>(entity);
            Assert.AreEqual(world.GetArchetype(entity), world.GetArchetype(Archetype.CreateDefinition(typeof(ExampleComponent))));
            world.AddComponentImmediate<ExampleTransform>(entity);
            Assert.AreEqual(world.GetArchetype(entity), world.GetArchetype(Archetype.CreateDefinition(typeof(ExampleComponent), typeof(ExampleTransform))));
            world.RemoveComponentImmediate<ExampleComponent>(entity);
            Assert.AreEqual(world.GetArchetype(entity), world.GetArchetype(Archetype.CreateDefinition(typeof(ExampleTransform))));
            world.RemoveComponentImmediate<ExampleTransform>(entity);
            Assert.AreEqual(world.GetArchetype(entity), world.GetArchetype(Archetype.CreateDefinition(Array.Empty<Type>())));
        }

        [Test]
        public void EntityTest()
        {
            var entity = world.Pack(world.CreateEntityImmediate());
            Assert.AreEqual(world.GetArchetype(entity.Entity), world.GetArchetype(Archetype.CreateDefinition(Array.Empty<Type>())));
            world.DestroyEntityImmediate(entity.Entity);
            var e2 = world.Pack(world.CreateEntityImmediate());
            Assert.AreEqual(entity.Entity, e2.Entity);
            Assert.AreEqual(entity.World, e2.World);
            Assert.AreEqual(entity.Special, e2.Special);
            Assert.AreNotEqual(entity.Version, e2.Version);
        }

        [Test]
        public void EntityComponentTest()
        {
            var entity = world.CreateEntityImmediate();
            Assert.AreEqual(world.GetArchetype(entity), world.GetArchetype(Archetype.CreateDefinition(Array.Empty<Type>())));
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
            world.DestroyEntityImmediate(entity.Entity);
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
    }
}
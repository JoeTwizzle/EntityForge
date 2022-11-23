using Archie.Helpers;

namespace Archie.Tests
{
    struct ExampleComponent : IComponent<ExampleComponent>
    {
        public int Number;
    }

    struct ExampleTransform : IComponent<ExampleTransform>
    {
        public float X, Y, Z;

        public ExampleTransform(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }
    }

    public class Tests
    {
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
            Assert.AreEqual(world.GetArchetype(entity), world.GetArchetype(Array.Empty<Type>()));
            world.AddComponentImmediate<ExampleComponent>(entity);
            Assert.AreEqual(world.GetArchetype(entity), world.GetArchetype(new Type[] { typeof(ExampleComponent) }));
            world.AddComponentImmediate<ExampleTransform>(entity);
            Assert.AreEqual(world.GetArchetype(entity), world.GetArchetype(new Type[] { typeof(ExampleComponent), typeof(ExampleTransform) }));
            world.RemoveComponentImmediate<ExampleComponent>(entity);
            Assert.AreEqual(world.GetArchetype(entity), world.GetArchetype(new Type[] { typeof(ExampleTransform) }));
            world.RemoveComponentImmediate<ExampleTransform>(entity);
            Assert.AreEqual(world.GetArchetype(entity), world.GetArchetype(Array.Empty<Type>()));
        }

        [Test]
        public void EntityTest()
        {
            var entity = world.CreateEntityImmediate();
            Assert.AreEqual(world.GetArchetype(entity), world.GetArchetype(Array.Empty<Type>()));
            world.DestroyEntityImmediate(entity);
            var e2 = world.CreateEntityImmediate();
            Assert.AreEqual(entity.Entity, e2.Entity);
            Assert.AreEqual(entity.World, e2.World);
            Assert.AreEqual(entity.Special, e2.Special);
            Assert.AreNotEqual(entity.Version, e2.Version);
        }

        [Test]
        public void EntityComponentTest()
        {
            var entity = world.CreateEntityImmediate();
            Assert.AreEqual(world.GetArchetype(entity), world.GetArchetype(Array.Empty<Type>()));
            world.AddComponentImmediate<ExampleComponent>(entity);
            world.AddComponentImmediate<ExampleTransform>(entity);
#if DEBUG
            Assert.Throws<DuplicateComponentException>(() => world.AddComponentImmediate<ExampleTransform>(entity));
#else
            Assert.Throws<ArgumentException>(() => world.AddComponentImmediate<ExampleTransform>(entity));
#endif
            world.DestroyEntityImmediate(entity);
        }

        [Test]
        public void EntityCreateTest()
        {
            var entity = world.CreateEntityImmediate();
            world.DestroyEntityImmediate(entity);
            var e2 = world.CreateEntityImmediate();
            Assert.AreEqual(entity.Entity, e2.Entity);
            Assert.AreEqual(entity.World, e2.World);
            Assert.AreEqual(entity.Special, e2.Special);
            Assert.AreNotEqual(entity.Version, e2.Version);
        }

        [Test]
        public void EntityComponentTest2()
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
        public void EntityComponentTest3()
        {
            var entity = world.CreateEntityImmediate();
            world.AddComponentImmediate<ExampleComponent>(entity);
            world.AddComponentImmediate<ExampleTransform>(entity);
            world.DestroyEntityImmediate(entity);
            var e2 = world.CreateEntityImmediate();
            Assert.Throws<ArgumentException>(() => world.AddComponentImmediate<ExampleComponent>(entity));
            Assert.DoesNotThrow(() => world.AddComponentImmediate<ExampleTransform>(e2));
        }
    }
}
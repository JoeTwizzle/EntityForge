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
            var entity = world.CreateEntity();
            Assert.AreEqual(world.GetArchetype(entity), world.GetArchetype(archetypeC0));
            world.AddComponent<ExampleComponent>(entity);
            Assert.AreEqual(world.GetArchetype(entity), world.GetArchetype(ArchetypeDefinition.Create().Inc<ExampleComponent>().End()));
            world.AddComponent<ExampleTransform>(entity);
            Assert.AreEqual(world.GetArchetype(entity), world.GetArchetype(ArchetypeDefinition.Create().Inc<ExampleComponent>().Inc<ExampleTransform>().End()));
            world.RemoveComponent<ExampleComponent>(entity);
            Assert.AreEqual(world.GetArchetype(entity), world.GetArchetype(ArchetypeDefinition.Create().Inc<ExampleTransform>().End()));
            world.RemoveComponent<ExampleTransform>(entity);
            Assert.AreEqual(world.GetArchetype(entity), world.GetArchetype(archetypeC0));
        }

        [Test]
        public void EntityTest()
        {
            var entity = world.Pack(world.CreateEntity());
            Assert.AreEqual(world.GetArchetype(entity.ToEntityId()), world.GetArchetype(archetypeC0));
            world.DestroyEntity(entity.ToEntityId());
            var e2 = world.Pack(world.CreateEntity());
            Assert.AreEqual(entity.Entity, e2.Entity);
            Assert.AreEqual(entity.World, e2.World);
            //Assert.AreEqual(entity.Special, e2.Special);
            Assert.AreNotEqual(entity.Version, e2.Version);
        }

        [Test]
        public void CreateManyTest()
        {
            for (int i = 0; i < 10000; i++)
            {
                Assert.DoesNotThrow(() => world.CreateEntity(archetypeC1C2C3));
            }
        }

        [Test]
        public void EntityComponentTest()
        {
            var entity = world.CreateEntity();
            Assert.AreEqual(world.GetArchetype(entity), world.GetArchetype(archetypeC0));
            world.AddComponent<ExampleComponent>(entity);
            world.AddComponent<ExampleTransform>(entity);
#if DEBUG
            Assert.Throws<DuplicateComponentException>(() => world.AddComponent<ExampleTransform>(entity));
#else
            Assert.Throws<ArgumentException>(() => world.AddComponent<ExampleTransform>(entity));
#endif
            world.DestroyEntity(entity);
        }

        [Test]
        public void NewDestroyNewTest()
        {
            var entity = world.Pack(world.CreateEntity());
            world.DestroyEntity(entity.ToEntityId());
            var e2 = world.Pack(world.CreateEntity());
            Assert.AreEqual(entity.Entity, e2.Entity);
            Assert.AreEqual(entity.World, e2.World);
            //Assert.AreEqual(entity.Special, e2.Special);
            Assert.AreNotEqual(entity.Version, e2.Version);
        }

        [Test]
        public void NewAddDestroyAddTest()
        {
            var entity = world.CreateEntity();
            world.AddComponent<ExampleComponent>(entity);
            world.AddComponent<ExampleTransform>(entity);
            world.DestroyEntity(entity);
            var e2 = world.CreateEntity();
            Assert.DoesNotThrow(() => world.AddComponent<ExampleComponent>(e2));
            Assert.DoesNotThrow(() => world.AddComponent<ExampleTransform>(e2));
        }

        [Test]
        public void NewAddDestroyNewAddTest()
        {
            var entity = world.CreateEntity();
            world.AddComponent<ExampleComponent>(entity);
            world.AddComponent<ExampleTransform>(entity);
            world.DestroyEntity(entity);
            var e2 = world.CreateEntity();
#if DEBUG
            Assert.DoesNotThrow(() => world.AddComponent<ExampleComponent>(entity));
#else
            Assert.DoesNotThrow(() => world.AddComponent<ExampleComponent>(entity));
#endif
            Assert.DoesNotThrow(() => world.AddComponent<ExampleTransform>(e2));
        }

        [Test]
        public void NewAddRemoveAddTest()
        {
            var entity = world.CreateEntity();
            Assert.DoesNotThrow(() => world.AddComponent<ExampleComponent>(entity));
            Assert.DoesNotThrow(() => world.RemoveComponent<ExampleComponent>(entity));
            Assert.DoesNotThrow(() => world.AddComponent<ExampleComponent>(entity));
        }

        [Test]
        public void NewAddAddRemoveAddRemoveTest()
        {
            var entity = world.CreateEntity();
            Assert.DoesNotThrow(() => world.AddComponent<ExampleComponent>(entity));
            Assert.DoesNotThrow(() => world.AddComponent<ExampleTransform>(entity));
            Assert.DoesNotThrow(() => world.RemoveComponent<ExampleComponent>(entity));
            Assert.DoesNotThrow(() => world.AddComponent<ExampleComponent>(entity));
            Assert.DoesNotThrow(() => world.RemoveComponent<ExampleTransform>(entity));
        }

        [Test]
        public void MultiNewAddAddRemoveAddRemoveDestroyTest()
        {
            var entity = world.CreateEntity();
            Assert.DoesNotThrow(() => world.AddComponent<ExampleComponent>(entity));
            Assert.DoesNotThrow(() => world.AddComponent<ExampleTransform>(entity));
            Assert.DoesNotThrow(() => world.RemoveComponent<ExampleComponent>(entity));
            Assert.DoesNotThrow(() => world.AddComponent<ExampleComponent>(entity));
            Assert.DoesNotThrow(() => world.RemoveComponent<ExampleTransform>(entity));
            world.DestroyEntity(entity);
            entity = world.CreateEntity();
            Assert.DoesNotThrow(() => world.AddComponent<ExampleComponent>(entity));
            Assert.DoesNotThrow(() => world.AddComponent<ExampleTransform>(entity));
            Assert.DoesNotThrow(() => world.RemoveComponent<ExampleComponent>(entity));
            Assert.DoesNotThrow(() => world.AddComponent<ExampleComponent>(entity));
            Assert.DoesNotThrow(() => world.RemoveComponent<ExampleTransform>(entity));
        }

        EntityId[] InitMany(int count, ArchetypeDefinition def = default)
        {
            if (def == default)
            {
                def = archetypeC1C2;
            }
            EntityId[] entites;
            entites = new EntityId[iterations];
            world = new World();
            //world.ReserveEntities(archetypeC1C2, iterations);
            for (int i = 0; i < iterations; i++)
            {
                entites[i] = world.CreateEntity(def);
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
                Assert.DoesNotThrow(() => world.AddComponent<Component3>(ents[i]));
            }
        }

        [Test]
        public void RemoveManyTest()
        {
            var ents = InitMany(iterations);

            for (int i = 0; i < ents.Length; i++)
            {
                Assert.DoesNotThrow(() => world.RemoveComponent<Component1>(ents[i]));
            }
        }

        [Test]
        public void RemoveManyTest2()
        {
            var ents = InitMany(iterations);

            for (int i = 0; i < ents.Length; i++)
            {
                Assert.DoesNotThrow(() => world.RemoveComponent<Component2>(ents[i]));
            }
        }

        [Test]
        public void DeferAddTest()
        {
            var ents = InitMany(iterations);
            Assert.AreEqual(iterations, world.GetArchetype(archetypeC1C2)?.Entities.Length ?? 0);
            Assert.AreEqual(0, world.GetArchetype(archetypeC1C2C3)?.Entities.Length ?? 0);
            ComponentMask mask1 = ComponentMask.Create().Read<Component1>().Read<Component2>().End();
            world.Query(mask1, arch =>
            {
                var ents = arch.Entities;
                for (int i = 0; i < ents.Length; i++)
                {
                    ents[i].AddComponent<Component3>();
                }
            });
            Assert.AreEqual(0, world.GetArchetype(archetypeC1C2)?.Entities.Length ?? 0);
            Assert.AreEqual(iterations, world.GetArchetype(archetypeC1C2C3)?.Entities.Length ?? 0);
        }

        [Test]
        public void DeferAddMultipleTest()
        {
            var ents = InitMany(iterations, archetypeC1);
            Assert.AreEqual(iterations, world.GetArchetype(archetypeC1)?.Entities.Length ?? 0);
            Assert.AreEqual(0, world.GetArchetype(archetypeC1C2C3)?.Entities.Length ?? 0);
            ComponentMask mask1 = ComponentMask.Create().Read<Component1>().End();
            world.Query(mask1, arch =>
            {
                var ents = arch.Entities;
                for (int i = 0; i < ents.Length; i++)
                {
                    ents[i].AddComponent<Component2>();
                    ents[i].AddComponent<Component3>();
                }
            });
            Assert.AreEqual(0, world.GetArchetype(archetypeC1)?.Entities.Length ?? 0);
            Assert.AreEqual(iterations, world.GetArchetype(archetypeC1C2C3)?.Entities.Length ?? 0);
        }

        [Test]
        public void DeferAddValueTest()
        {
            var ents = InitMany(iterations);
            Assert.AreEqual(iterations, world.GetArchetype(archetypeC1C2)?.Entities.Length ?? 0);
            Assert.AreEqual(0, world.GetArchetype(archetypeC1C2C3)?.Entities.Length ?? 0);
            ComponentMask mask1 = ComponentMask.Create().Read<Component1>().Read<Component2>().End();
            world.Query(mask1, arch =>
            {
                var ents = arch.Entities;
                for (int i = 0; i < ents.Length; i++)
                {
                    ents[i].AddComponent<Component3>(new Component3() { Value = 1337 });
                }
            });
            Assert.AreEqual(0, world.GetArchetype(archetypeC1C2)?.Entities.Length ?? 0);
            Assert.AreEqual(iterations, world.GetArchetype(archetypeC1C2C3)?.Entities.Length ?? 0);
            ComponentMask mask2 = ComponentMask.Create().Read<Component1>().Read<Component2>().Read<Component3>().End();
            world.Query(mask2, arch =>
            {
                var c3s = arch.GetRead<Component3>();
                for (int i = 0; i < c3s.Length; i++)
                {
                    Assert.AreEqual(1337, c3s[i].Value);
                }
            });
        }

        [Test]
        public void DeferRemoveTest()
        {
            var ents = InitMany(iterations);
            Assert.AreEqual(iterations, world.GetArchetype(archetypeC1C2)?.Entities.Length ?? 0);
            Assert.AreEqual(0, world.GetArchetype(archetypeC1)?.Entities.Length ?? 0);
            ComponentMask mask1 = ComponentMask.Create().Read<Component1>().Read<Component2>().End();
            world.Query(mask1, arch =>
            {
                var ents = arch.Entities;
                for (int i = 0; i < ents.Length; i++)
                {
                    ents[i].RemoveComponent<Component2>();
                }
            });
            Assert.AreEqual(0, world.GetArchetype(archetypeC1C2)?.Entities.Length ?? 0);
            Assert.AreEqual(iterations, world.GetArchetype(archetypeC1)?.Entities.Length ?? 0);
        }

        [Test]
        public void DeferRemoveMultipleTest()
        {
            var ents = InitMany(iterations, archetypeC1C2C3);
            Assert.AreEqual(iterations, world.GetArchetype(archetypeC1C2C3)?.Entities.Length ?? 0);
            Assert.AreEqual(0, world.GetArchetype(archetypeC1)?.Entities.Length ?? 0);
            ComponentMask mask1 = ComponentMask.Create().Read<Component1>().End();
            world.Query(mask1, arch =>
            {
                var ents = arch.Entities;
                for (int i = 0; i < ents.Length; i++)
                {
                    ents[i].RemoveComponent<Component2>();
                    ents[i].RemoveComponent<Component3>();
                }
            });
            Assert.AreEqual(0, world.GetArchetype(archetypeC1C2C3)?.Entities.Length ?? 0);
            Assert.AreEqual(iterations, world.GetArchetype(archetypeC1)?.Entities.Length ?? 0);
        }

        [Test]
        public void DeferDestroyTest()
        {
            var ents = InitMany(iterations);
            Assert.AreEqual(iterations, world.GetArchetype(archetypeC1C2)?.Entities.Length ?? 0);
            ComponentMask mask = ComponentMask.Create().Read<Component1>().Read<Component2>().End();
            world.Query(mask, arch =>
            {
                var ents = arch.Entities;
                for (int i = 0; i < ents.Length; i++)
                {
                    ents[i].Destroy();
                }
            });
            Assert.AreEqual(0, world.GetArchetype(archetypeC1C2)?.Entities.Length ?? 0);
        }
    }
}
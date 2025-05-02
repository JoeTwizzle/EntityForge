using EntityForge.Helpers;

namespace EntityForge.Tests
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
            Assert.That(world.GetArchetype(entity), Is.EqualTo(world.GetArchetype(archetypeC0)));
            world.AddComponent<ExampleComponent>(entity);
            Assert.That(world.GetArchetype(entity), Is.EqualTo(world.GetArchetype(ArchetypeDefinition.Create().Inc<ExampleComponent>().End())));
            world.AddComponent<ExampleTransform>(entity);
            Assert.That(world.GetArchetype(entity), Is.EqualTo(world.GetArchetype(ArchetypeDefinition.Create().Inc<ExampleComponent>().Inc<ExampleTransform>().End())));
            world.RemoveComponent<ExampleComponent>(entity);
            Assert.That(world.GetArchetype(entity), Is.EqualTo(world.GetArchetype(ArchetypeDefinition.Create().Inc<ExampleTransform>().End())));
            world.RemoveComponent<ExampleTransform>(entity);
            Assert.That(world.GetArchetype(entity), Is.EqualTo(world.GetArchetype(archetypeC0)));
        }

        [Test]
        public void EntityTest()
        {
            var entity = world.CreateEntity();
            Assert.That(world.GetArchetype(entity.ToEntityId()), Is.EqualTo(world.GetArchetype(archetypeC0)));
            world.DeleteEntity(entity.ToEntityId());
            var e2 = world.CreateEntity();
            Assert.That(e2.EntityId, Is.EqualTo(entity.EntityId));
            Assert.That(e2.World, Is.EqualTo(entity.World));
            //Assert.AreEqual(entity.Special, e2.Special);
            Assert.That(e2.Version, Is.Not.EqualTo(entity.Version));
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
        public void CreateManyWorldsTest()
        {
            for (int i = 0; i < 100; i++)
            {
                Assert.DoesNotThrow(() =>
                {
                    var w = new World();
                    for (int i = 0; i < 10000; i++)
                    {
                        Assert.DoesNotThrow(() => world.CreateEntity());
                    }
                    w.Dispose();
                });
            }
        }

        [Test]
        public void EntityComponentTest()
        {
            var entity = world.CreateEntity();
            Assert.That(world.GetArchetype(entity), Is.EqualTo(world.GetArchetype(archetypeC0)));
            world.AddComponent<ExampleComponent>(entity);
            world.AddComponent<ExampleTransform>(entity);
#if DEBUG
            Assert.Throws<DuplicateComponentException>(() => world.AddComponent<ExampleTransform>(entity));
#else
            Assert.Throws<ArgumentException>(() => world.AddComponent<ExampleTransform>(entity));
#endif
            world.DeleteEntity(entity);
        }

        [Test]
        public void NewDestroyNewTest()
        {
            var entity = world.CreateEntity();
            world.DeleteEntity(entity.ToEntityId());
            var e2 = world.CreateEntity();
            Assert.That(e2.EntityId, Is.EqualTo(entity.EntityId));
            Assert.That(e2.World, Is.EqualTo(entity.World));
            //Assert.AreEqual(entity.Special, e2.Special);
            Assert.That(e2.Version, Is.Not.EqualTo(entity.Version));
        }

        [Test]
        public void NewAddDestroyAddTest()
        {
            var entity = world.CreateEntity();
            world.AddComponent<ExampleComponent>(entity);
            world.AddComponent<ExampleTransform>(entity);
            world.DeleteEntity(entity);
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
            world.DeleteEntity(entity);
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
            world.DeleteEntity(entity);
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
            world.ReserveEntities(def, iterations);
            for (int i = 0; i < iterations; i++)
            {
                entites[i] = world.CreateEntity(def);
            }
            return entites;
        }

        int iterations = 100000;
        [Test]
        public void InitializedToDefault()
        {
            var ents = InitMany(iterations);
            for (int i = 0; i < ents.Length; i++)
            {
                Assert.That(default(Component2), Is.EqualTo(world.GetComponent<Component2>(ents[i])));
            }
        }

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
            Assert.That(world.GetArchetype(archetypeC1C2)?.Entities.Length ?? 0, Is.EqualTo(iterations));
            Assert.That(world.GetArchetype(archetypeC1C2C3)?.Entities.Length ?? 0, Is.EqualTo(0));
            ComponentMask mask1 = ComponentMask.Create().Read<Component1>().Read<Component2>().End();
            world.Query(mask1, arch =>
            {
                var ents = arch.Entities;
                for (int i = 0; i < ents.Length; i++)
                {
                    ents[i].AddComponent<Component3>();
                }
            });
            Assert.That(world.GetArchetype(archetypeC1C2)?.Entities.Length ?? 0, Is.EqualTo(0));
            Assert.That(world.GetArchetype(archetypeC1C2C3)?.Entities.Length ?? 0, Is.EqualTo(iterations));
        }

        [Test]
        public void DeferAddMultipleTest()
        {
            var ents = InitMany(iterations, archetypeC1);
            Assert.That(world.GetArchetype(archetypeC1)?.Entities.Length ?? 0, Is.EqualTo(iterations));
            Assert.That(world.GetArchetype(archetypeC1C2C3)?.Entities.Length ?? 0, Is.EqualTo(0));
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
            Assert.That(world.GetArchetype(archetypeC1)?.Entities.Length ?? 0, Is.EqualTo(0));
            Assert.That(world.GetArchetype(archetypeC1C2C3)?.Entities.Length ?? 0, Is.EqualTo(iterations));
        }

        [Test]
        public void DeferAddValueTest()
        {
            var ents = InitMany(iterations);
            Assert.That(world.GetArchetype(archetypeC1C2)?.Entities.Length ?? 0, Is.EqualTo(iterations));
            Assert.That(world.GetArchetype(archetypeC1C2C3)?.Entities.Length ?? 0, Is.EqualTo(0));
            ComponentMask mask1 = ComponentMask.Create().Read<Component1>().Read<Component2>().End();
            world.Query(mask1, arch =>
            {
                var ents = arch.Entities;
                for (int i = 0; i < ents.Length; i++)
                {
                    ents[i].AddComponent<Component3>(new Component3() { Value = 1337 });
                }
            });
            Assert.That(world.GetArchetype(archetypeC1C2)?.Entities.Length ?? 0, Is.EqualTo(0));
            Assert.That(world.GetArchetype(archetypeC1C2C3)?.Entities.Length ?? 0, Is.EqualTo(iterations));
            ComponentMask mask2 = ComponentMask.Create().Read<Component1>().Read<Component2>().Read<Component3>().End();
            world.Query(mask2, arch =>
            {
                var c3s = arch.GetRead<Component3>();
                for (int i = 0; i < c3s.Length; i++)
                {
                    Assert.That(c3s[i].Value, Is.EqualTo(1337));
                }
            });
        }

        [Test]
        public void DeferAddValueGetTest()
        {
            var ents = InitMany(iterations);
            Assert.That(world.GetArchetype(archetypeC1C2)?.Entities.Length ?? 0, Is.EqualTo(iterations));
            Assert.That(world.GetArchetype(archetypeC1C2C3)?.Entities.Length ?? 0, Is.EqualTo(0));
            ComponentMask mask1 = ComponentMask.Create().Read<Component1>().Read<Component2>().End();
            world.Query(mask1, arch =>
            {
                var ents = arch.Entities;
                for (int i = 0; i < ents.Length; i++)
                {
                    ents[i].AddComponent<Component3>(new Component3() { Value = 1337 });
                    ents[i].GetComponent<Component3>();
                }
            });
            Assert.That(world.GetArchetype(archetypeC1C2)?.Entities.Length ?? 0, Is.EqualTo(0));
            Assert.That(world.GetArchetype(archetypeC1C2C3)?.Entities.Length ?? 0, Is.EqualTo(iterations));
            ComponentMask mask2 = ComponentMask.Create().Read<Component1>().Read<Component2>().Read<Component3>().End();
            world.Query(mask2, arch =>
            {
                var c3s = arch.GetRead<Component3>();
                for (int i = 0; i < c3s.Length; i++)
                {
                    Assert.That(c3s[i].Value, Is.EqualTo(1337));
                }
            });
        }

        [Test]
        public void DeferAddTwoValueTest()
        {
            var ents = InitMany(iterations, archetypeC1);
            Assert.That(world.GetArchetype(archetypeC1)?.Entities.Length ?? 0, Is.EqualTo(iterations));
            Assert.That(world.GetArchetype(archetypeC1C2C3)?.Entities.Length ?? 0, Is.EqualTo(0));
            ComponentMask mask1 = ComponentMask.Create().Read<Component1>().End();
            world.Query(mask1, arch =>
            {
                var ents = arch.Entities;
                for (int i = 0; i < ents.Length; i++)
                {
                    ents[i].AddComponent<Component2>(new Component2() { Value = 420 });
                    ents[i].AddComponent<Component3>(new Component3() { Value = 1337 });
                }
            });
            Assert.That(world.GetArchetype(archetypeC1)?.Entities.Length ?? 0, Is.EqualTo(0));
            Assert.That(world.GetArchetype(archetypeC1C2C3)?.Entities.Length ?? 0, Is.EqualTo(iterations));
            ComponentMask mask2 = ComponentMask.Create().Read<Component1>().Read<Component2>().Read<Component3>().End();
            world.Query(mask2, arch =>
            {
                var c2s = arch.GetRead<Component2>();
                for (int i = 0; i < c2s.Length; i++)
                {
                    Assert.That(c2s[i].Value, Is.EqualTo(420));
                }
                var c3s = arch.GetRead<Component3>();
                for (int i = 0; i < c3s.Length; i++)
                {
                    Assert.That(c3s[i].Value, Is.EqualTo(1337));
                }
            });
        }

        [Test]
        public void DeferAddRemoveValueTest()
        {
            var ents = InitMany(iterations);
            Assert.That(world.GetArchetype(archetypeC1C2)?.Entities.Length ?? 0, Is.EqualTo(iterations));
            Assert.That(world.GetArchetype(archetypeC1C2C3)?.Entities.Length ?? 0, Is.EqualTo(0));
            ComponentMask mask1 = ComponentMask.Create().Read<Component1>().Read<Component2>().End();
            world.Query(mask1, arch =>
            {
                var ents = arch.Entities;
                for (int i = 0; i < ents.Length; i++)
                {
                    ents[i].AddComponent<Component3>(new Component3() { Value = 1337 });
                    ents[i].RemoveComponent<Component3>();
                }
            });
            Assert.That(world.GetArchetype(archetypeC1C2)?.Entities.Length ?? 0, Is.EqualTo(iterations));
            Assert.That(world.GetArchetype(archetypeC1C2C3)?.Entities.Length ?? 0, Is.EqualTo(0));
        }

        [Test]
        public void DeferRemoveAddValueTest()
        {
            var ents = InitMany(iterations);
            Assert.That(world.GetArchetype(archetypeC1C2)?.Entities.Length ?? 0, Is.EqualTo(iterations));
            Assert.That(world.GetArchetype(archetypeC1C2C3)?.Entities.Length ?? 0, Is.EqualTo(0));
            ComponentMask mask1 = ComponentMask.Create().Read<Component1>().Read<Component2>().End();
            world.Query(mask1, arch =>
            {
                var ents = arch.Entities;
                for (int i = 0; i < ents.Length; i++)
                {
                    Assert.That(ents[i].HasComponent<Component2>());
                    ents[i].RemoveComponent<Component2>();
                    Assert.That(!ents[i].HasComponent<Component2>());
                    ents[i].AddComponent<Component2>(new Component2() { Value = 1337 });
                    Assert.That(ents[i].HasComponent<Component2>());
                }
            });
            Assert.That(world.GetArchetype(archetypeC1C2)?.Entities.Length ?? 0, Is.EqualTo(iterations));
            Assert.That(world.GetArchetype(archetypeC1C2C3)?.Entities.Length ?? 0, Is.EqualTo(0));
        }

        [Test]
        public void DeferRemoveTest()
        {
            var ents = InitMany(iterations);
            Assert.That(world.GetArchetype(archetypeC1C2)?.Entities.Length ?? 0, Is.EqualTo(iterations));
            Assert.That(world.GetArchetype(archetypeC1)?.Entities.Length ?? 0, Is.EqualTo(0));
            ComponentMask mask1 = ComponentMask.Create().Read<Component1>().Read<Component2>().End();
            world.Query(mask1, arch =>
            {
                var ents = arch.Entities;
                for (int i = 0; i < ents.Length; i++)
                {
                    ents[i].RemoveComponent<Component2>();
                }
            });
            Assert.That(world.GetArchetype(archetypeC1C2)?.Entities.Length ?? 0, Is.EqualTo(0));
            Assert.That(world.GetArchetype(archetypeC1)?.Entities.Length ?? 0, Is.EqualTo(iterations));
        }

        [Test]
        public void DeferRemoveMultipleTest()
        {
            var ents = InitMany(iterations, archetypeC1C2C3);
            Assert.That(world.GetArchetype(archetypeC1C2C3)?.Entities.Length ?? 0, Is.EqualTo(iterations));
            Assert.That(world.GetArchetype(archetypeC1)?.Entities.Length ?? 0, Is.EqualTo(0));
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
            Assert.That(world.GetArchetype(archetypeC1C2C3)?.Entities.Length ?? 0, Is.EqualTo(0));
            Assert.That(world.GetArchetype(archetypeC1)?.Entities.Length ?? 0, Is.EqualTo(iterations));
        }

        [Test]
        public void DeferDestroyTest()
        {
            var ents = InitMany(iterations);
            Assert.That(world.GetArchetype(archetypeC1C2)?.Entities.Length ?? 0, Is.EqualTo(iterations));
            ComponentMask mask = ComponentMask.Create().Read<Component1>().Read<Component2>().End();
            world.Query(mask, arch =>
            {
                var ents = arch.Entities;
                for (int i = 0; i < ents.Length; i++)
                {
                    ents[i].Delete();
                }
            });
            Assert.That(world.GetArchetype(archetypeC1C2)?.Entities.Length ?? 0, Is.EqualTo(0));
        }

        [Test]
        public void DeferCreateTest()
        {
            Assert.That(world.GetArchetype(archetypeC1C2)?.Entities.Length ?? 0, Is.EqualTo(0));
            var arch = world.GetOrCreateArchetype(archetypeC1C2)!;
            arch.Lock();
            for (int i = 0; i < iterations; i++)
            {
                world.CreateEntity(archetypeC1C2);
            }
            arch.Unlock();
            Assert.That(world.GetArchetype(archetypeC1C2)?.Entities.Length ?? 0, Is.EqualTo(iterations));
        }
    }
}
using Archie.Jobs;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Archie.Tests
{

    internal class MultiThreadingTests
    {
        readonly static ArchetypeDefinition archetypeC0 = ArchetypeBuilder.Create().End();
        readonly static ArchetypeDefinition archetypeC1 = ArchetypeBuilder.Create().Inc<Component1>().End();
        readonly static ArchetypeDefinition archetypeC2 = ArchetypeBuilder.Create().Inc<Component2>().End();
        readonly static ArchetypeDefinition archetypeC3 = ArchetypeBuilder.Create().Inc<Component3>().End();
        readonly static ArchetypeDefinition archetypeC1C2 = ArchetypeBuilder.Create().Inc<Component1>().Inc<Component2>().End();
        readonly static ArchetypeDefinition archetypeC1C3 = ArchetypeBuilder.Create().Inc<Component1>().Inc<Component3>().End();
        readonly static ArchetypeDefinition archetypeC2C3 = ArchetypeBuilder.Create().Inc<Component2>().Inc<Component3>().End();
        readonly static ArchetypeDefinition archetypeC1C2C3 = ArchetypeBuilder.Create().Inc<Component1>().Inc<Component2>().Inc<Component3>().End();
        readonly static ComponentMask mask1 = ComponentMask.Create().Write<Component1>().End();
        readonly static ComponentMask mask1x2 = ComponentMask.Create().Write<Component1>().Exc<Component2>().End();
        readonly static ComponentMask mask2 = ComponentMask.Create().Read<Component1>().Read<Component2>().End();
        readonly static ComponentMask mask3 = ComponentMask.Create().Read<Component1>().Read<Component2>().Read<Component3>().End();
        readonly static ComponentMask maskSome1x2 = ComponentMask.Create().Some().Write<Component1>().Read<Component2>().EndSome().End();
        readonly static ComponentMask maskSome1_2 = ComponentMask.Create().Some().Write<Component1>().Read<Component2>().EndSome().Some().Write<Component2>().Read<Component3>().EndSome().End();
        World world;
        JobThreadScheduler scheduler;

        const int entityIterations = 1000;

        [SetUp]
        public void Setup()
        {
            world = new World();
            for (int i = 0; i < entityIterations; i++)
            {
                world.CreateEntity(archetypeC0);
                world.CreateEntity(archetypeC1);
                world.CreateEntity(archetypeC2);
                world.CreateEntity(archetypeC3);
                world.CreateEntity(archetypeC1C2);
                world.CreateEntity(archetypeC1C3);
                world.CreateEntity(archetypeC2C3);
                world.CreateEntity(archetypeC1C2C3);
            }
            scheduler = new();
        }

        [Test]
        public void ScheduleTest()
        {
            var handle1 = scheduler.Schedule(() => world.Query(mask1, arch =>
            {
                var c1s = arch.GetWrite<Component1>();
                for (int i = 0; i < c1s.Length; i++)
                {
                    c1s[i].Value++;
                }
            }));

            var handle2 = scheduler.Schedule(() => world.Query(mask1, arch =>
            {
                var c1s = arch.GetWrite<Component1>();
                for (int i = 0; i < c1s.Length; i++)
                {
                    c1s[i].Value++;
                }
            }));
            handle1.WaitForCompletion();
            handle2.WaitForCompletion();
            int count = 0;
            var filter = world.GetFilter(mask1);
            for (int i = 0; i < filter.MatchCount; i++)
            {
                var pool = filter.MatchingArchetypes[i].GetPool<Component1>();
                for (int j = 0; j < pool.Length; j++)
                {
                    count += pool[j].Value;
                    Assert.AreEqual(2, pool[j].Value);
                }
            }
            Assert.AreEqual(entityIterations * 4 * 2, count);
        }


        [Test]
        public void MTQuery()
        {
            world.QueryParallel(mask1x2, arch =>
            {
                var c1s = arch.GetWrite<Component1>();
                for (int i = 0; i < c1s.Length; i++)
                {
                    c1s[i].Value = 1;
                }
            });

            int count = 0;

            var filter = world.GetFilter(mask1x2);
            for (int i = 0; i < filter.MatchCount; i++)
            {
                var pool = filter.MatchingArchetypes[i].GetPool<Component1>();
                for (int j = 0; j < pool.Length; j++)
                {
                    count += pool[j].Value;
                }
            }
            Assert.AreEqual(2000, count);
        }
    }
}

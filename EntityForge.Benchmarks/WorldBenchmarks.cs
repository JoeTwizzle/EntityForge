﻿using BenchmarkDotNet.Attributes;

namespace EntityForge.Benchmarks
{

    [Config(typeof(MyConfig))]
    [MemoryDiagnoser]
    public class WorldBenchmarks
    {
        [Params(10000)]
        public int iterations { get; set; }
        static readonly ArchetypeDefinition archetypeC0 = ArchetypeBuilder.Create().End();
        static readonly ArchetypeDefinition archetypeC1 = ArchetypeBuilder.Create().Inc<Component1>().End();
        static readonly ArchetypeDefinition archetypeC2 = ArchetypeBuilder.Create().Inc<Component2>().End();
        static readonly ArchetypeDefinition archetypeC3 = ArchetypeBuilder.Create().Inc<Component3>().End();
        static readonly ArchetypeDefinition archetypeC1C2 = ArchetypeBuilder.Create().Inc<Component1>().Inc<Component2>().End();
        static readonly ArchetypeDefinition archetypeC1C3 = ArchetypeBuilder.Create().Inc<Component1>().Inc<Component3>().End();
        static readonly ArchetypeDefinition archetypeC2C3 = ArchetypeBuilder.Create().Inc<Component2>().Inc<Component3>().End();
        static readonly ArchetypeDefinition archetypeC1C2C3 = ArchetypeBuilder.Create().Inc<Component1>().Inc<Component2>().Inc<Component3>().End();
        static readonly ComponentMask mask1 = ComponentMask.Create().Read<Component1>().End();

        [Benchmark]
        public void AddTwoValuesDeferred()
        {
            var world = new World();
            world.ReserveEntities(archetypeC1, iterations);
            for (int i = 0; i < iterations; i++)
            {
                world.CreateEntity(archetypeC1);
            }
            world.Query(mask1, arch =>
            {
                var ents = arch.Entities;
                for (int i = 0; i < ents.Length; i++)
                {
                    ents[i].AddComponent<Component2>(new Component2() { Value = 420 });
                    ents[i].AddComponent<Component3>(new Component3() { Value = 1337 });
                }
            });
        }

        [Benchmark(Baseline = true)]
        public void CreateEntityWithOneComponent()
        {
            var world = new World();
            world.ReserveEntities(archetypeC1, iterations);
            for (int i = 0; i < iterations; i++)
            {
                world.CreateEntity(archetypeC1);
            }
        }

        [Benchmark]
        public void CreateEntityWithTwoComponent()
        {
            var world = new World();
            world.ReserveEntities(archetypeC1C2, iterations);
            for (int i = 0; i < iterations; i++)
            {
                world.CreateEntity(archetypeC1C2);
            }
        }

        [Benchmark]
        public void CreateEntityWithThreeComponent()
        {
            var world = new World();
            world.ReserveEntities(archetypeC1C2C3, iterations);
            for (int i = 0; i < iterations; i++)
            {
                world.CreateEntity(archetypeC1C2C3);
            }
        }

        [Benchmark]
        public void CreateEntityWithOneComponentSimple()
        {
            var world = new World();
            for (int i = 0; i < iterations; i++)
            {
                world.CreateEntity(archetypeC1);
            }
        }

        [Benchmark]
        public void CreateEntityWithTwoComponentSimple()
        {
            var world = new World();
            for (int i = 0; i < iterations; i++)
            {
                world.CreateEntity(archetypeC1C2);
            }
        }

        [Benchmark]
        public void CreateEntityWithThreeComponentSimple()
        {
            var world = new World();
            for (int i = 0; i < iterations; i++)
            {
                world.CreateEntity(archetypeC1C2C3);
            }
        }
    }
}

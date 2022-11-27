using BenchmarkDotNet.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Archie.Benchmarks
{
    public class FilterBenchmarks
    {
        [Params(100000)]
        public uint iterations { get; set; }
        ArchetypeDefinition archetypeC0 = Archetype.CreateDefinition(new Type[] { });
        ArchetypeDefinition archetypeC1 = Archetype.CreateDefinition(new Type[] { typeof(Component1) });
        ArchetypeDefinition archetypeC2 = Archetype.CreateDefinition(new Type[] { typeof(Component2) });
        ArchetypeDefinition archetypeC3 = Archetype.CreateDefinition(new Type[] { typeof(Component3) });
        ArchetypeDefinition archetypeC1C2 = Archetype.CreateDefinition(new Type[] { typeof(Component1), typeof(Component2) });
        ArchetypeDefinition archetypeC1C3 = Archetype.CreateDefinition(new Type[] { typeof(Component1), typeof(Component3) });
        ArchetypeDefinition archetypeC2C3 = Archetype.CreateDefinition(new Type[] { typeof(Component2), typeof(Component3) });
        ArchetypeDefinition archetypeC1C2C3 = Archetype.CreateDefinition(new Type[] { typeof(Component1), typeof(Component2), typeof(Component3) });
        World world;
        [GlobalSetup]
        public void Setup()
        {
            world = new World();
            world.ReserveEntities(archetypeC1, iterations);
            world.ReserveEntities(archetypeC1C2, iterations);
            world.ReserveEntities(archetypeC1C2C3, iterations);
            for (int i = 0; i < iterations; i++)
            {
                world.CreateEntityImmediate(archetypeC1);
                world.CreateEntityImmediate(archetypeC1C2);
                world.CreateEntityImmediate(archetypeC1C2C3);
            }
        }

        [Benchmark]
        public void SystemWithOneComponent()
        {
            var filter = world.FilterInc<Component1>().End();
            foreach (var entity in filter)
            {
                world.GetComponent<Component1>(entity).Value++;
            }
        }

        [Benchmark]
        public void SystemWithTwoComponents()
        {
            var filter = world.FilterInc<Component1>().Inc<Component2>().End();
            foreach (var entity in filter)
            {
                world.GetComponent<Component1>(entity).Value++;
            }
        }

        [Benchmark]
        public void SystemWithThreeComponents()
        {
            var filter = world.FilterInc<Component1>().Inc<Component2>().Inc<Component3>().End();
            foreach (var entity in filter)
            {
                world.GetComponent<Component1>(entity).Value++;
            }
        }
    }
}

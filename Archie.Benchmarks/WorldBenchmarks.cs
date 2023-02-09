using BenchmarkDotNet.Attributes;

namespace Archie.Benchmarks
{
    struct Component1 : IComponent<Component1>
    {
        public int Value;
    }

    public struct Component2 : IComponent<Component2>
    {
        public int Value;
    }

    public struct Component3 : IComponent<Component3>
    {
        public int Value;
    }

    [MemoryDiagnoser]
    //[HardwareCounters(BenchmarkDotNet.Diagnosers.HardwareCounter.CacheMisses)]
    public class WorldBenchmarks
    {
        [Params(100000)]
        public int iterations { get; set; }
        ArchetypeDefinition archetypeC0 = Archetype.CreateDefinition(new Type[] { });
        ArchetypeDefinition archetypeC1 = Archetype.CreateDefinition(new Type[] { typeof(Component1) });
        ArchetypeDefinition archetypeC2 = Archetype.CreateDefinition(new Type[] { typeof(Component2) });
        ArchetypeDefinition archetypeC3 = Archetype.CreateDefinition(new Type[] { typeof(Component3) });
        ArchetypeDefinition archetypeC1C2 = Archetype.CreateDefinition(new Type[] { typeof(Component1), typeof(Component2) });
        ArchetypeDefinition archetypeC1C3 = Archetype.CreateDefinition(new Type[] { typeof(Component1), typeof(Component3) });
        ArchetypeDefinition archetypeC2C3 = Archetype.CreateDefinition(new Type[] { typeof(Component2), typeof(Component3) });
        ArchetypeDefinition archetypeC1C2C3 = Archetype.CreateDefinition(new Type[] { typeof(Component1), typeof(Component2), typeof(Component3) });

        [Benchmark]
        public void CreateEntityWithOneComponent()
        {
            var world = new World();
            world.ReserveEntities(archetypeC1, iterations);
            for (int i = 0; i < iterations; i++)
            {
                world.CreateEntityImmediate(archetypeC1);
            }
        }

        [Benchmark]
        public void CreateEntityWithTwoComponent()
        {
            var world = new World();
            world.ReserveEntities(archetypeC1C2, iterations);
            for (int i = 0; i < iterations; i++)
            {
                world.CreateEntityImmediate(archetypeC1C2);
            }
        }

        [Benchmark]
        public void CreateEntityWithThreeComponent()
        {
            var world = new World();
            world.ReserveEntities(archetypeC1C2C3, iterations);
            for (int i = 0; i < iterations; i++)
            {
                world.CreateEntityImmediate(archetypeC1C2C3);
            }
        }

        [Benchmark]
        public void CreateEntityWithOneComponentSimple()
        {
            var world = new World();
            for (int i = 0; i < iterations; i++)
            {
                world.CreateEntityImmediate(archetypeC1);
            }
        }

        [Benchmark]
        public void CreateEntityWithTwoComponentSimple()
        {
            var world = new World();
            for (int i = 0; i < iterations; i++)
            {
                world.CreateEntityImmediate(archetypeC1C2);
            }
        }

        [Benchmark]
        public void CreateEntityWithThreeComponentSimple()
        {
            var world = new World();
            for (int i = 0; i < iterations; i++)
            {
                world.CreateEntityImmediate(archetypeC1C2C3);
            }
        }
    }
}

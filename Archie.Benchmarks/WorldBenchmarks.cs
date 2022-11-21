using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet;
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
    [HardwareCounters(BenchmarkDotNet.Diagnosers.HardwareCounter.CacheMisses)]
    public class WorldBenchmarks
    {
        [Params(100000)]
        public int iterations { get; set; }
        Type[] archetype = new Type[] { };
        Type[] archetypeC1 = new Type[] { typeof(Component1) };

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
    }
}

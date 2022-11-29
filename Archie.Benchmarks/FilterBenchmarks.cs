using BenchmarkDotNet.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
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
        ComponentMask mask1 = ComponentMask.Create().Inc<Component1>().End();
        ComponentMask mask2 = ComponentMask.Create().Inc<Component1>().Inc<Component2>().End();
        ComponentMask mask3 = ComponentMask.Create().Inc<Component1>().Inc<Component2>().Inc<Component3>().End();

        [GlobalSetup]
        public void Setup()
        {
            world = new World();
            world.ReserveEntities(archetypeC1, iterations);
            for (int i = 0; i < iterations; i++)
            {
                world.CreateEntityImmediate(archetypeC1);
            }
        }
        private IncrementValueSystem incrementValueSystem;
        private struct IncrementValueSystem : IComponentQuery<Component1>
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Process(ref Component1 t0)
            {
                ++t0.Value;
            }
        }

        [Benchmark]
        public void SystemWithOneComponent()
        {
            var filter = world.Filter(mask1);
            foreach (var entity in filter)
            {
                
            }
        }
    }
}

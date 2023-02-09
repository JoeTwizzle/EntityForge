using BenchmarkDotNet.Attributes;
using System.Diagnostics.CodeAnalysis;

namespace Archie.Benchmarks
{
    [MemoryDiagnoser]
    public class ComponentBenchmarks
    {
        [Params(100000)]
        public int iterations { get; set; }
        ArchetypeDefinition archetypeC0 = Archetype.CreateDefinition(Array.Empty<Type>());
        ArchetypeDefinition archetypeC1 = Archetype.CreateDefinition(new Type[] { typeof(Component1) });
        ArchetypeDefinition archetypeC2 = Archetype.CreateDefinition(new Type[] { typeof(Component2) });
        ArchetypeDefinition archetypeC3 = Archetype.CreateDefinition(new Type[] { typeof(Component3) });
        ArchetypeDefinition archetypeC1C2 = Archetype.CreateDefinition(new Type[] { typeof(Component1), typeof(Component2) });
        ArchetypeDefinition archetypeC1C3 = Archetype.CreateDefinition(new Type[] { typeof(Component1), typeof(Component3) });
        ArchetypeDefinition archetypeC2C3 = Archetype.CreateDefinition(new Type[] { typeof(Component2), typeof(Component3) });
        ArchetypeDefinition archetypeC1C2C3 = Archetype.CreateDefinition(new Type[] { typeof(Component1), typeof(Component2), typeof(Component3) });
        [AllowNull]
        World world;
        ComponentMask mask1 = ComponentMask.Create().Inc<Component1>().End();
        ComponentMask mask2 = ComponentMask.Create().Inc<Component1>().Inc<Component2>().End();
        ComponentMask mask3 = ComponentMask.Create().Inc<Component1>().Inc<Component2>().Inc<Component3>().End();
        [AllowNull]
        EntityId[] entites;

        [IterationSetup]
        public void Setup()
        {
            entites = new EntityId[iterations];
            world = new World();
            world.ReserveEntities(archetypeC1C2, iterations);
            for (int i = 0; i < iterations; i++)
            {
                entites[i] = world.CreateEntityImmediate(archetypeC1C2);
            }
        }


        [Benchmark]
        public void AddComponent()
        {
            for (int i = 0; i < entites.Length; i++)
            {
                world.AddComponentImmediate<Component3>(entites[i]);
            }
        }

        [Benchmark]
        public void RemoveComponent()
        {
            for (int i = 0; i < entites.Length; i++)
            {
                world.RemoveComponentImmediate<Component1>(entites[i]);
            }
        }

        [Benchmark]
        public void SetComponent()
        {
            for (int i = 0; i < entites.Length; i++)
            {
                world.SetComponentImmediate<Component3>(entites[i]);
            }
        }

        [Benchmark]
        public void UnsetComponent()
        {
            for (int i = 0; i < entites.Length; i++)
            {
                world.UnsetComponentImmediate<Component1>(entites[i]);
            }
        }
    }
}

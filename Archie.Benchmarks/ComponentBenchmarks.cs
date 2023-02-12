﻿using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.InProcess.NoEmit;
using System.Diagnostics.CodeAnalysis;

namespace Archie.Benchmarks
{
    public class AntiVirusFriendlyConfig : ManualConfig
    {
        public AntiVirusFriendlyConfig()
        {
            AddJob(Job.MediumRun
                .WithToolchain(InProcessNoEmitToolchain.Instance));
        }
    }
    //[Config(typeof(AntiVirusFriendlyConfig))]
    [MemoryDiagnoser]
    public class ComponentBenchmarks
    {
        [Params(10000000)]
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
        World[] worlds;
        ComponentMask mask1 = ComponentMask.Create().Inc<Component1>().End();
        ComponentMask mask2 = ComponentMask.Create().Inc<Component1>().Inc<Component2>().End();
        ComponentMask mask3 = ComponentMask.Create().Inc<Component1>().Inc<Component2>().Inc<Component3>().End();
        [AllowNull]
        EntityId[][] entites;
        const int numWorlds = 4;

        [IterationSetup]
        public void Setup()
        {
            worlds = new World[numWorlds];
            entites = new EntityId[numWorlds][];
            for (int worldId = 0; worldId < numWorlds; worldId++)
            {
                entites[worldId] = new EntityId[iterations];
                worlds[worldId] = new World();
                worlds[worldId].ReserveEntities(archetypeC1C2, iterations);
                for (int i = 0; i < iterations; i++)
                {
                    entites[worldId][i] = worlds[worldId].CreateEntityImmediate(archetypeC1C2);
                }
            }
        }

        [Benchmark]
        public void AddComponent()
        {
            for (int i = 0; i < iterations; i++)
            {
                worlds[0].AddComponentImmediate<Component3>(entites[0][i]);
            }
        }

        [Benchmark]
        public void RemoveComponent()
        {
            for (int i = 0; i < iterations; i++)
            {
                worlds[1].RemoveComponentImmediate<Component1>(entites[1][i]);
            }
        }

        [Benchmark]
        public void SetComponent()
        {
            for (int i = 0; i < iterations; i++)
            {
                worlds[2].SetComponentImmediate<Component3>(entites[2][i]);
            }
        }

        [Benchmark]
        public void UnsetComponent()
        {
            for (int i = 0; i < iterations; i++)
            {
                worlds[3].UnsetComponentImmediate<Component1>(entites[3][i]);
            }
        }
    }
}
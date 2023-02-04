using BenchmarkDotNet.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;

namespace Archie.Benchmarks
{
    [MemoryDiagnoser]
    public partial class FilterBenchmarks
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

        [GlobalSetup]
        public void Setup()
        {
            world = new World();
            world.ReserveEntities(archetypeC1, iterations);
            for (int i = 0; i < iterations; i++)
            {
                world.CreateEntityImmediate(archetypeC1);
            }
            world.ReserveEntities(archetypeC1C2, iterations);
            for (int i = 0; i < iterations; i++)
            {
                world.CreateEntityImmediate(archetypeC1C2);
            }
            world.ReserveEntities(archetypeC1C2C3, iterations);
            for (int i = 0; i < iterations; i++)
            {
                world.CreateEntityImmediate(archetypeC1C2C3);
            }
        }
        private QC1 qc1;
        private struct QC1 : IComponentQuery<Component1>
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Process(ref Component1 c1)
            {
                ++c1.Value;
            }
        }
        private QC2 qc2;
        private struct QC2 : IComponentQuery<Component1, Component2>
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Process(ref Component1 c1, ref Component2 c2)
            {
                ++c1.Value;
                ++c2.Value;
            }
        }
        private QC3 qc3;
        private struct QC3 : IComponentQuery<Component1, Component2, Component3>
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Process(ref Component1 c1, ref Component2 c2, ref Component3 c3)
            {
                ++c1.Value;
                ++c2.Value;
                ++c3.Value;
            }
        }

        [Archie.InjectTypes]
        private ref partial struct Group1
        {
            public readonly ref Component1 c1;
        }
        [Archie.InjectTypes]
        private ref partial struct Group2
        {
            public readonly ref Component1 c1;
            public readonly ref Component2 c2;
        }
        [Archie.InjectTypes]
        private ref partial struct Group3
        {
            public readonly ref Component1 c1;
            public readonly ref Component2 c2;
            public readonly ref Component3 c3;
        }


        public void FilterWithOneComponent()
        {
            var filter = world.GetFilter(mask1);
            foreach (var entity in filter)
            {
                ++world.GetComponent<Component1>(entity).Value;
            }
        }

        public void IterateWithOneComponent()
        {
            foreach (var archetype in world.GetMatchingArchetypes(mask1))
            {
                foreach (var group in Group1.GetIterator(archetype))
                {
                    ++group.c1.Value;
                }
            }
        }

        public void QueryWithOneComponent()
        {
            world.Query<QC1, Component1>(mask1, ref qc1);
        }

        public void QueryV2WithOneComponent()
        {
            world.Query<Component1>(mask1, (length, c1) =>
            {
                for (int i = 0; i < length; i++)
                {
                    ++c1[i].Value;
                }
            });
        }


        public void FilterWithTwoComponents()
        {
            var filter = world.GetFilter(mask2);
            foreach (var entity in filter)
            {
                ++world.GetComponent<Component1>(entity).Value;
                ++world.GetComponent<Component2>(entity).Value;
            }
        }


        public void IterateWithTwoComponents()
        {
            foreach (var archetype in world.GetMatchingArchetypes(mask2))
            {
                foreach (var group in Group2.GetIterator(archetype))
                {
                    ++group.c1.Value;
                    ++group.c2.Value;
                }
            }
        }


        public void QueryWithTwoComponents()
        {
            world.Query<QC2, Component1, Component2>(mask2, ref qc2);
        }

        public void QueryV2WithTwoComponents()
        {
            world.Query<Component1, Component2>(mask2, (length, c1, c2) =>
            {
                for (int i = 0; i < length; i++)
                {
                    ++c1[i].Value;
                    ++c2[i].Value;
                }
            });
        }


        public void FilterWithThreeComponents()
        {
            var filter = world.GetFilter(mask3);
            foreach (var entity in filter)
            {
                ++world.GetComponent<Component1>(entity).Value;
                ++world.GetComponent<Component2>(entity).Value;
                ++world.GetComponent<Component3>(entity).Value;
            }
        }

        public void IterateWithThreeComponents()
        {
            foreach (var archetype in world.GetMatchingArchetypes(mask3))
            {
                foreach (var group in Group3.GetIterator(archetype))
                {
                    ++group.c1.Value;
                    ++group.c2.Value;
                    ++group.c3.Value;
                }
            }
        }
        [Benchmark]
        public void QueryV2WithThreeComponents()
        {
            world.Query<Component1, Component2, Component3>(mask3, (length, c1, c2, c3) =>
            {
                for (int i = 0; i < length; i++)
                {
                    ++c1[i].Value;
                    ++c2[i].Value;
                    ++c3[i].Value;
                }
            });
        }
        [Benchmark]
        public void QueryWithThreeComponents()
        {
            world.Query<QC3, Component1, Component2, Component3>(mask3);
        }
        [Benchmark]
        public void QueryFuncWithThreeComponents()
        {
            world.Query<Component1, Component2, Component3>(mask3, (a, b, c) =>
            {
                ++a.Value.Value;
                ++b.Value.Value;
                ++c.Value.Value;
            });
        }
    }
}

using BenchmarkDotNet.Attributes;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace EntityForge.Benchmarks
{
    [MemoryDiagnoser]
    //[Config(typeof(MyConfig))]
    public partial class FilterBenchmarks
    {
        [Params(100000)]
        public int iterations { get; set; }
        ArchetypeDefinition archetypeC0 = ArchetypeBuilder.Create().End();
        ArchetypeDefinition archetypeC1 = ArchetypeBuilder.Create().Inc<Component1>().End();
        ArchetypeDefinition archetypeC2 = ArchetypeBuilder.Create().Inc<Component2>().End();
        ArchetypeDefinition archetypeC3 = ArchetypeBuilder.Create().Inc<Component3>().End();
        ArchetypeDefinition archetypeC1C2 = ArchetypeBuilder.Create().Inc<Component1>().Inc<Component2>().End();
        ArchetypeDefinition archetypeC1C3 = ArchetypeBuilder.Create().Inc<Component1>().Inc<Component3>().End();
        ArchetypeDefinition archetypeC2C3 = ArchetypeBuilder.Create().Inc<Component2>().Inc<Component3>().End();
        ArchetypeDefinition archetypeC1C2C3 = ArchetypeBuilder.Create().Inc<Component1>().Inc<Component2>().Inc<Component3>().End();
        [AllowNull]
        World world;
        ComponentMask mask1 = ComponentMask.Create().Read<Component1>().End();
        ComponentMask mask2 = ComponentMask.Create().Read<Component1>().Read<Component2>().End();
        ComponentMask mask3 = ComponentMask.Create().Read<Component1>().Read<Component2>().Read<Component3>().End();

        [GlobalSetup]
        public void Setup()
        {
            world = new World();
            world.ReserveEntities(archetypeC1, iterations);
            for (int i = 0; i < iterations; i++)
            {
                world.CreateEntity(archetypeC1);
            }
            world.ReserveEntities(archetypeC1C2, iterations);
            for (int i = 0; i < iterations; i++)
            {
                world.CreateEntity(archetypeC1C2);
            }
            world.ReserveEntities(archetypeC1C2C3, iterations);
            for (int i = 0; i < iterations; i++)
            {
                world.CreateEntity(archetypeC1C2C3);
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

        //[Archie.InjectTypes]
        //private ref partial struct Group1
        //{
        //    public readonly ref Component1 c1;
        //}
        //[Archie.InjectTypes]
        //private ref partial struct Group2
        //{
        //    public readonly ref Component1 c1;
        //    public readonly ref Component2 c2;
        //}
        //[Archie.InjectTypes]
        //private ref partial struct Group3
        //{
        //    public readonly ref Component1 c1;
        //    public readonly ref Component2 c2;
        //    public readonly ref Component3 c3;
        //}


        public void FilterWithOneComponent()
        {
            var filter = world.GetArchetypeFilter(mask1);
            foreach (var entity in filter)
            {
                ++world.GetComponent<Component1>(entity).Value;
            }
        }


        [Benchmark]
        public void QueryWithOneComponent()
        {
            world.Query<QC1, Component1>(mask1, ref qc1);
        }
        //[Benchmark]
        //public void QueryV2WithOneComponent()
        //{
        //    world.Query<Component1>(mask1, (length, c1) =>
        //    {
        //        for (int i = 0; i < length; i++)
        //        {
        //            ++c1[i].Value;
        //        }
        //    });
        //}
        //[Benchmark]
        //public void QueryV3WithOneComponent()
        //{
        //    world.Query<Component1>(mask1, (ComponentRef<Component1> c1) =>
        //    {
        //        ++(c1.Value).Value;
        //    });
        //}

        public void FilterWithTwoComponents()
        {
            var filter = world.GetArchetypeFilter(mask2);
            foreach (var entity in filter)
            {
                ++world.GetComponent<Component1>(entity).Value;
                ++world.GetComponent<Component2>(entity).Value;
            }
        }



        [Benchmark]
        public void QueryWithTwoComponents()
        {
            world.Query<QC2, Component1, Component2>(mask2, ref qc2);
        }
        //[Benchmark]
        //public void QueryV2WithTwoComponents()
        //{
        //    world.Query<Component1, Component2>(mask2, (length, c1, c2) =>
        //    {
        //        for (int i = 0; i < length; i++)
        //        {
        //            ++c1[i].Value;
        //            ++c2[i].Value;
        //        }
        //    });
        //}

        //[Benchmark]
        //public void QueryV3WithTwoComponents()
        //{
        //    world.Query<Component1, Component2>(mask2, (ComponentRef<Component1> c1, ComponentRef<Component2> c2) =>
        //    {
        //        ++(c1.Value).Value;
        //        ++(c2.Value).Value;
        //    });
        //}


        public void FilterWithThreeComponents()
        {
            var filter = world.GetArchetypeFilter(mask3);
            foreach (var entity in filter)
            {
                ++world.GetComponent<Component1>(entity).Value;
                ++world.GetComponent<Component2>(entity).Value;
                ++world.GetComponent<Component3>(entity).Value;
            }
        }

        [Benchmark]
        public void QueryWithThreeComponents()
        {
            world.Query<QC3, Component1, Component2, Component3>(mask3);
        }
    }
}

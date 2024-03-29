﻿using BenchmarkDotNet.Attributes;
using System.Diagnostics.CodeAnalysis;

namespace EntityForge.Benchmarks
{
    //[Config(typeof(MyConfig))]
    [MemoryDiagnoser]
    public class QueryBenchmarks
    {
        [Params(10000, 100000, 1000000)] public int Amount;

        [AllowNull]
        private static World world;
        static readonly ArchetypeDefinition archDef = ArchetypeBuilder.Create().Inc<Position2>().Inc<Velocity2>().End();
        static readonly ComponentMask queryMask = ComponentMask.Create().Write<Position2>().Read<Velocity2>().End();

        [GlobalSetup]
        public void Setup()
        {
            world = new World();
            world.ReserveEntities(archDef, Amount);

            for (var index = 0; index < Amount; index++)
            {
                var entity = world.CreateEntity(archDef);
                world.SetComponent(entity, new Position2 { X = 0, Y = 0 });
                world.SetComponent(entity, new Velocity2 { X = 1, Y = 1 });
            }
        }

        [Benchmark]
        public void Query()
        {
            world.Query(queryMask, static (arch) =>
            {
                var v2s = arch.GetRead<Velocity2>();
                var p2s = arch.GetWrite<Position2>();
                for (int i = 0; i < arch.Length; i++)
                {
                    p2s[i].X += v2s[i].X;
                    p2s[i].Y += v2s[i].Y;
                }
            });
        }

        [Benchmark]
        public void QueryStruct()
        {
            world.Query<PosVelQuery, Position2, Velocity2>(queryMask);
        }

        struct PosVelQuery : IComponentQuery<Position2, Velocity2>
        {
            public void Process(ref Position2 pos, ref Velocity2 vel)
            {
                pos.X += vel.X;
                pos.Y += vel.Y;
            }
        }
    }
}

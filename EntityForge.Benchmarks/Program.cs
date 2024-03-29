﻿using BenchmarkDotNet.Running;

namespace EntityForge.Benchmarks
{
    internal sealed class Program
    {
        static void Main(string[] args)
        {

            //if (args.Length == 0)
            //{
            //    args = new string[] { "--profiler", "EP" };
            //}
            var summary = BenchmarkSwitcher.FromTypes(new Type[] { typeof(FilterBenchmarks), typeof(WorldBenchmarks), typeof(QueryBenchmarks) })
            //.Run(args, DefaultConfig.Instance.AddDiagnoser(new EtwProfiler()));
            .Run(args);

            Console.WriteLine("Done Running!!!");
            Console.ReadLine();
        }
    }
}
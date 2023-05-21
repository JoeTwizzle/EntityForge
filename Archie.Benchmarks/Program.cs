using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnostics.Windows;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;

namespace Archie.Benchmarks
{
    internal sealed class Program
    {
        static void Main(string[] args)
        {

            //if (args.Length == 0)
            //{
            //    args = new string[] { "--profiler", "EP" };
            //}
            var summary = BenchmarkSwitcher.FromTypes(new Type[] { typeof(FilterBenchmarks), typeof(WorldBenchmarks) })
              //.Run(args, DefaultConfig.Instance.AddDiagnoser(new EtwProfiler()));
            .Run(args);

            Console.WriteLine("Done Running!!!");
            Console.ReadLine();
        }
    }
}
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnostics.Windows;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.InProcess.Emit;

namespace Archie.Benchmarks
{
    internal sealed class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                args = new string[] { "--profiler", "EP" };
            }
            var summary = BenchmarkSwitcher.FromTypes(new Type[] { typeof(ComponentBenchmarks) })
              .Run(args, DefaultConfig.Instance.AddDiagnoser(new EtwProfiler())); // HERE

            Console.WriteLine("Done Running!!!");
            Console.ReadLine();
        }
    }
}
using BenchmarkDotNet.Running;

namespace Archie.Benchmarks
{
    internal sealed class Program
    {
        static void Main(string[] args)
        {
            //BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
            //BenchmarkRunner.Run<WorldBenchmarks>();
            BenchmarkRunner.Run<ComponentBenchmarks>();
            //BenchmarkRunner.Run<FilterBenchmarks>();
            Console.WriteLine("Done Running!!!");
            Console.ReadLine();
        }
    }
}
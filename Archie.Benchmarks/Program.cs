using BenchmarkDotNet.Running;

namespace Archie.Benchmarks
{
    internal class Program
    {
        static void Main(string[] args)
        {
            BenchmarkRunner.Run<FilterBenchmarks>();
            //BenchmarkRunner.Run<WorldBenchmarks>();
            Console.WriteLine("Done Running!!!");
            Console.ReadLine();
        }
    }
}
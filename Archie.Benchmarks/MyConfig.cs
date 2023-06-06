using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;

namespace EntityForge.Benchmarks
{
    // Custom config to define "Default vs PGO"
    class MyConfig : ManualConfig
    {
        public MyConfig()
        {
            // Use .NET 7.0 default mode:
            //AddJob(Job.Default.WithId("Default mode"));

            // Use Dynamic PGO mode:
            AddJob(Job.Default.WithId("Dynamic PGO")
                .WithEnvironmentVariables(
                    new EnvironmentVariable("DOTNET_TieredPGO", "1"),
                    new EnvironmentVariable("DOTNET_TC_QuickJitForLoops", "1"),
                    new EnvironmentVariable("DOTNET_ReadyToRun", "0")));
        }
    }
}
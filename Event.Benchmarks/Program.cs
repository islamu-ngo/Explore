// ABOUTME: BenchmarkDotNet entrypoint for running Event benchmark suites from the command line.
// ABOUTME: Discovers benchmark classes in this assembly and forwards BenchmarkDotNet CLI arguments.

using BenchmarkDotNet.Running;

namespace Event.Benchmarks;

public class Program
{
    public static void Main(string[] args)
    {
        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
    }
}

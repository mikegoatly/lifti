using BenchmarkDotNet.Running;

namespace PerformanceProfiling
{
    internal static class Program
    {
        public static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run<SerializationBenchmarks>();
        }
    }
}

using BenchmarkDotNet.Running;
using System;

namespace PerformanceProfiling
{
    internal static class Program
    {
        public static void Main(string[] args)
        {
            BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
        }
    }
}

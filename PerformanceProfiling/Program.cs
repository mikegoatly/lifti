using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Lifti;
using System;

namespace PerformanceProfiling
{
    internal static class Program
    {
        public static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run<FullTextIndexTests>();
        }
    }
}

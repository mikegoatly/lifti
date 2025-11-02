using BenchmarkDotNet.Attributes;
using Lifti;
using Lifti.Tokenization;
using System;
using System.Linq;
using System.Text;

namespace PerformanceProfiling
{
    public class TokenStoreMergeOrAddBenchmarks : IndexBenchmarkBase
    {
        private TokenStore tokenStore = new();
        private readonly string[] tokenTexts = [.. Enumerable.Range(0, 10000).Select(i => $"token{i}")];
        private readonly TokenLocation sampleLocation = new(1, 1, 1);

        [IterationSetup]
        public void IterationSetup()
        {
            this.tokenStore = new TokenStore();
        }
        [Benchmark]
        public object MergeOrAddTokens_AllUnique()
        {
            foreach (var tokenText in this.tokenTexts)
            {
                this.tokenStore.MergeOrAdd(tokenText.AsMemory(), this.sampleLocation);
            }

            return true;
        }

        [Benchmark]
        public object MergeOrAddTokens_AllDuplicates()
        {
            for (var i = 0; i < 100; i++)
            {
                foreach (var tokenText in this.tokenTexts.Take(100))
                {
                    this.tokenStore.MergeOrAdd(tokenText.AsMemory(), this.sampleLocation);
                }
            }

            return true;
        }
    }
}

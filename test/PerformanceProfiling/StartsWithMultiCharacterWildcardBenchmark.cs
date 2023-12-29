using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Lifti;
using System.Threading.Tasks;

namespace PerformanceProfiling
{
    [RankColumn, MemoryDiagnoser]
    public class StartsWithMultiCharacterWildcardBenchmark : IndexBenchmarkBase
    {
        private IFullTextIndex<int> index;

        [GlobalSetup]
        public async Task SetUp()
        {
            this.index = CreateNewIndex(4);
            await this.PopulateIndexAsync(this.index);
        }

        [Benchmark]
        public object Searching()
        {
            return this.index.Search("*ion");
        }
    }
}

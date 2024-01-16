using BenchmarkDotNet.Attributes;
using Lifti;
using System.Threading.Tasks;

namespace PerformanceProfiling
{
    public class StartsWithMultiCharacterWildcardBenchmark : IndexBenchmarkBase
    {
        private readonly IFullTextIndex<int> index = CreateNewIndex(4);

        [GlobalSetup]
        public async Task SetUp()
        {
            await this.PopulateIndexAsync(this.index);
        }

        [Benchmark]
        public object Searching()
        {
            return this.index.Search("*ion");
        }
    }
}

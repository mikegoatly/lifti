using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Lifti;
using System.Threading.Tasks;

namespace PerformanceProfiling
{
    [ShortRunJob(RuntimeMoniker.Net481)]
    [ShortRunJob(RuntimeMoniker.Net80)]
    [ShortRunJob(RuntimeMoniker.Net70)]
    [ShortRunJob(RuntimeMoniker.Net60)]
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

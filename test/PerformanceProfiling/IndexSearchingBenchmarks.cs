using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Lifti;
using System.Threading.Tasks;

namespace PerformanceProfiling
{
    //[ShortRunJob(RuntimeMoniker.Net481)]
    [ShortRunJob(RuntimeMoniker.Net70)]
    //[ShortRunJob(RuntimeMoniker.Net60)]
    [RankColumn, MemoryDiagnoser]
    public class IndexSearchingBenchmarks : IndexBenchmarkBase
    {
        private IFullTextIndex<string> index;

        [GlobalSetup]
        public async Task SetUp()
        {
            this.index = CreateNewIndex(4);
            await this.PopulateIndexAsync(this.index);
        } 

        [Params(
            "(confiscation & th*) | \"and they\"",
            "*",
            "th*",
            "and they also",
            "?and ?they ?also",
            "confiscation",
            "and | they",
            "and ~ they"
            )]
        public string SearchCriteria { get; set; }

        [Benchmark]
        public object Searching()
        {
            return this.index.Search(this.SearchCriteria);
        }
    }
}

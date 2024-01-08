using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Lifti;
using System.Threading.Tasks;

namespace PerformanceProfiling
{
    [RankColumn, MemoryDiagnoser]
    public class IndexSearchingBenchmarks : IndexBenchmarkBase
    {
        private IFullTextIndex<int> index;

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
            "con??*",
            "Title=?great",
            "confiscation",
            "co*on",
            "and > they",
            "and ~10> they",
            "\"also has a\"",
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

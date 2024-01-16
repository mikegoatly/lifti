using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Lifti;
using System.Threading.Tasks;

namespace PerformanceProfiling
{
    public class IndexSearchingBenchmarks : IndexBenchmarkBase
    {
        private readonly IFullTextIndex<int> index = CreateNewIndex(4);

        [GlobalSetup]
        public async Task SetUp()
        {
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
        public string SearchCriteria { get; set; } = null!;

        [Benchmark]
        public object Searching()
        {
            return this.index.Search(this.SearchCriteria);
        }
    }
}

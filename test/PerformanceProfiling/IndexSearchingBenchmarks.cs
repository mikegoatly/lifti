using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Lifti;
using System.Threading.Tasks;

namespace PerformanceProfiling
{
    //public class AntiVirusFriendlyConfig : ManualConfig
    //{
    //    public AntiVirusFriendlyConfig()
    //    {
    //        this.

    //        AddJob(Job.ShortRun
    //        .With
    //            .WithToolchain(InProcessNoEmitToolchain.Instance));
    //    }
    //}

    [ShortRunJob(RuntimeMoniker.Net481)]
    [ShortRunJob(RuntimeMoniker.Net70)]
    [ShortRunJob(RuntimeMoniker.Net60)]
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

    //[ShortRunJob(RuntimeMoniker.Net481)]
    [ShortRunJob(RuntimeMoniker.Net70)]
    //[ShortRunJob(RuntimeMoniker.Net60)]
    [RankColumn, MemoryDiagnoser]
    public class StartsWithMultiCharacterWildcard : IndexBenchmarkBase
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

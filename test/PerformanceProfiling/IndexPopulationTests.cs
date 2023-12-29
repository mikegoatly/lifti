using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using System.Threading.Tasks;

namespace PerformanceProfiling
{
    [RankColumn, MemoryDiagnoser]
    public class IndexPopulationTests : IndexBenchmarkBase
    {
        [Benchmark()]
        public async Task IndexingAlwaysSupportIntraNodeText()
        {
            var index = CreateNewIndex(0);
            await this.PopulateIndexAsync(index);
        }

        //[Benchmark()]
        //public async Task IndexingAlwaysIndexCharByChar()
        //{
        //    var index = CreateNewIndex(1000);
        //    await this.PopulateIndexAsync(index);
        //}

        [Benchmark()]
        public async Task IndexingIntraNodeTextAt4Characters()
        {
            var index = CreateNewIndex(4);
            await this.PopulateIndexAsync(index);
        }

        //[Benchmark()]
        //public async Task IndexingOneByOneIntraNodeTextAt2Characters()
        //{
        //    var index = CreateNewIndex(2);
        //    await this.PopulateIndexOneByOneAsync(index);
        //}

        [Benchmark()]
        public async Task IndexingOneByOneAlwaysSupportIntraNodeText()
        {
            var index = CreateNewIndex(0);
            await this.PopulateIndexOneByOneAsync(index);
        }

        //[Benchmark()]
        //public async Task IndexingOneByOneAlwaysIndexCharByChar()
        //{
        //    var index = CreateNewIndex(1000);
        //    await this.PopulateIndexOneByOneAsync(index);
        //}

        [Benchmark()]
        public async Task IndexingOneByOneIntraNodeTextAt4Characters()
        {
            var index = CreateNewIndex(4);
            await this.PopulateIndexOneByOneAsync(index);
        }

        //[Benchmark()]
        //public async Task IndexingIntraNodeTextAt2Characters()
        //{
        //    var index = CreateNewIndex(2);
        //    await this.PopulateIndexAsync(index);
        //}
    }
}

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using System.Threading.Tasks;

namespace PerformanceProfiling
{
    public class IndexPopulationTests : IndexBenchmarkBase
    {
        // This is the default configuration for an index
        [Benchmark()]
        public async Task IndexingIntraNodeTextAt4Characters()
        {
            var index = CreateNewIndex(4);
            await this.PopulateIndexAsync(index);
        }
    }
}

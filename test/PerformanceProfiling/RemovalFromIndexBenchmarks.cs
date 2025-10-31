using BenchmarkDotNet.Attributes;
using Lifti;
using System.Threading.Tasks;

namespace PerformanceProfiling
{
    public class RemovalFromIndexBenchmarks : IndexBenchmarkBase
    {
        private readonly IFullTextIndex<int> index = CreateNewIndex(4);

        [GlobalSetup]
        public async Task SetUp()
        {
            await this.PopulateIndexAsync(this.index);
        }

        [Benchmark]
        public async Task<object> SingleRemoval()
        {
            await this.index.RemoveAsync(20);
            return true;
        }

        [Benchmark]
        public async Task<object> MutipleRemoval()
        {
            for (var i = 20; i < 31; i++)
            {
                await this.index.RemoveAsync(20);
            }

            return true;
        }

        [Benchmark]
        public async Task<object> BatchRemoval()
        {
            this.index.BeginBatchChange();

            for (var i = 20; i < 31; i++)
            {
                await this.index.RemoveAsync(20);
            }

            await this.index.CommitBatchChangeAsync();

            return true;
        }
    }
}

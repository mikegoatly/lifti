using BenchmarkDotNet.Attributes;
using Lifti;
using System.Threading.Tasks;

namespace PerformanceProfiling
{
    [RankColumn, MemoryDiagnoser]
    public class RemovalFromIndexBenchmarks : IndexBenchmarkBase
    {
        private IFullTextIndex<int> index;

        [GlobalSetup]
        public async Task SetUp()
        {
            this.index = CreateNewIndex(4);
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

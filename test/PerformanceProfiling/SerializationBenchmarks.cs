using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Lifti.Serialization.Binary;
using System;
using System.IO;
using System.Threading.Tasks;

namespace PerformanceProfiling
{
    [MediumRunJob(RuntimeMoniker.Net481)]
    [MediumRunJob(RuntimeMoniker.Net70)]
    [MediumRunJob(RuntimeMoniker.Net60)]
    [RankColumn, MemoryDiagnoser]
    public class SerializationBenchmarks : IndexBenchmarkBase
    {
        private BinarySerializer<int> serializer;
        private string fileName;

        [GlobalSetup]
        public async Task Setup()
        {
            var index = CreateNewIndex(2);
            await this.PopulateIndexAsync(index);

            this.serializer = new BinarySerializer<int>();
            this.fileName = $"{Guid.NewGuid()}.dat";
            using var stream = File.OpenWrite(this.fileName);
            await this.serializer.SerializeAsync(index, stream, true);
        }

        [Benchmark()]
        public async Task IndexDeserialization()
        {
            var index = CreateNewIndex(2);
            using var stream = File.OpenRead(this.fileName);
            await this.serializer.DeserializeAsync(index, stream, true);
        }
    }
}

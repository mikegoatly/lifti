using BenchmarkDotNet.Attributes;
using Lifti.Serialization.Binary;
using System;
using System.IO;
using System.Threading.Tasks;

namespace PerformanceProfiling
{
    public class SerializationBenchmarks : IndexBenchmarkBase
    {
        private readonly BinarySerializer<int> serializer = new();
        private readonly string fileName = $"{Guid.NewGuid()}.dat";

        [GlobalSetup]
        public async Task Setup()
        {
            var index = CreateNewIndex(2);
            await this.PopulateIndexAsync(index);

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

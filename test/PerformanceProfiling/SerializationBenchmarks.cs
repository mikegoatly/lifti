using BenchmarkDotNet.Attributes;
using Lifti;
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
        private readonly FullTextIndex<int> populatedIndex = CreateNewIndex(4);

        [GlobalSetup]
        public async Task Setup()
        {
            await this.PopulateIndexAsync(this.populatedIndex);

            using var stream = File.OpenWrite(this.fileName);
            await this.serializer.SerializeAsync(this.populatedIndex, stream, true);
        }

        [Benchmark()]
        public async Task IndexDeserialization()
        {
            var index = CreateNewIndex(4);
            using var stream = File.OpenRead(this.fileName);
            await this.serializer.DeserializeAsync(index, stream, true);
        }

        [Benchmark()]
        public async Task IndexSerialization()
        {
            var temporaryFile = Path.GetTempFileName();
            try
            {
                using var stream = File.OpenWrite(temporaryFile);
                await this.serializer.SerializeAsync(this.populatedIndex, stream, true);
            }
            finally
            {
                File.Delete(temporaryFile);
            }
        }
    }
}

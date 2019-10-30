using FluentAssertions;
using Lifti.Serialization.Binary;
using PerformanceProfiling;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Lifti.Tests.Serialization
{
    public class BinarySerializerTests
    {
        private readonly FullTextIndex<string> index;
        private readonly ITestOutputHelper output;

        public BinarySerializerTests(ITestOutputHelper output)
        {
            this.index = new FullTextIndexBuilder<string>().Build();
            var wikipediaTests = WikipediaDataLoader.Load(typeof(FullTextIndexTests));
            var options = new TokenizationOptionsBuilder().XmlContent().WithStemming().Build();
            foreach (var (name, text) in wikipediaTests)
            {
                this.index.Add(name, text, options);
            }

            this.output = output;
        }

        [Fact]
        public async Task ShouldRoundTripIndexStructure()
        {
            var serializer = new BinarySerializer<string>();

            var fileName = Guid.NewGuid().ToString() + ".dat";

            using (var stream = File.Open(fileName, FileMode.CreateNew))
            {
                var stopwatch = Stopwatch.StartNew();
                await serializer.SerializeAsync(this.index, stream, false);

                this.output.WriteLine($"Serialized in {stopwatch.ElapsedMilliseconds}ms");

                stream.Length.Should().BeGreaterThan(4);

                var newIndex = new FullTextIndexBuilder<string>().Build();

                stream.Position = 0;

                stopwatch.Restart();
                await serializer.DeserializeAsync(newIndex, stream, false);

                this.output.WriteLine($"Deserialized in {stopwatch.ElapsedMilliseconds}ms");

                newIndex.IdLookup.GetIndexedItems().Should().BeEquivalentTo(this.index.IdLookup.GetIndexedItems());
                newIndex.Count.Should().Be(this.index.Count);
                newIndex.Root.ToString().Should().Be(this.index.Root.ToString());

                var oldResults = index.Search("test").ToList();
                var newResults = newIndex.Search("test").ToList();

                oldResults.Should().NotBeEmpty();
                newResults.Should().BeEquivalentTo(oldResults);
            }

            File.Delete(fileName);
        }
    }
}

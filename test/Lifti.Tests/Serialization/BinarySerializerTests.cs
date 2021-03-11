using FluentAssertions;
using Lifti.Serialization.Binary;
using Lifti.Tokenization.TextExtraction;
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
        private readonly ITestOutputHelper output;

        public BinarySerializerTests(ITestOutputHelper output)
        {
            this.output = output;
        }

        [Fact]
        public async Task ShouldSerializeEmojiWithSurrogatePairs()
        {
            var index = await SearializeAndDeserializeIndexWithText("🎶 🤷🏾‍♀️");
            index.Search("🤷🏾‍♀️").Should().HaveCount(1);
        }

        [Fact]
        public async Task ShouldSerializeEmoji()
        {
            var index = await SearializeAndDeserializeIndexWithText("🎶");
            index.Search("🎶").Should().HaveCount(1);
        }

        [Fact]
        public async Task ShouldSerializeEmojiSequences()
        {
            var index = await SearializeAndDeserializeIndexWithText("🎶🤷🏾‍♀️");
            index.Search("🎶🤷🏾‍♀️").Should().HaveCount(1);
        }

        [Fact]
        public async Task ShouldDeserializeV3Index()
        {
            var index = new FullTextIndexBuilder<string>().Build();
            var serializer = new BinarySerializer<string>();
            using (var stream = new MemoryStream(TestResources.v3Index))
            {
                await serializer.DeserializeAsync(index, stream);
            }

            index.Search("serialized").Should().HaveCount(1);
            index.Search("亜").Should().HaveCount(1);
        }

        [Fact]
        public async Task ShouldDeserializeV2Index()
        {
            var index = new FullTextIndexBuilder<string>().Build();
            var serializer = new BinarySerializer<string>();
            using (var stream = new MemoryStream(TestResources.v2Index))
            {
                await serializer.DeserializeAsync(index, stream);
            }

            index.Search("serialized").Should().HaveCount(1);
            index.Search("亜").Should().HaveCount(1);
        }

        [Fact]
        public async Task ShouldRoundTripIndexStructure()
        {
            var serializer = new BinarySerializer<string>();

            var fileName = Guid.NewGuid().ToString() + ".dat";

            using (var stream = File.Open(fileName, FileMode.CreateNew))
            {
                var stopwatch = Stopwatch.StartNew();
                var index = await CreateWikipediaIndexAsync();
                await serializer.SerializeAsync(index, stream, false);

                this.output.WriteLine($"Serialized in {stopwatch.ElapsedMilliseconds}ms");

                stream.Length.Should().BeGreaterThan(4);

                var newIndex = new FullTextIndexBuilder<string>().Build();

                stream.Position = 0;

                stopwatch.Restart();
                await serializer.DeserializeAsync(newIndex, stream, false);

                this.output.WriteLine($"Deserialized in {stopwatch.ElapsedMilliseconds}ms");

                newIndex.Items.GetIndexedItems().Should().BeEquivalentTo(index.Items.GetIndexedItems());
                newIndex.Count.Should().Be(index.Count);
                newIndex.Root.ToString().Should().Be(index.Root.ToString());

                var oldResults = index.Search("test").ToList();
                var newResults = newIndex.Search("test").ToList();

                oldResults.Should().NotBeEmpty();
                newResults.Should().BeEquivalentTo(oldResults);

                newIndex.Search("🤷‍♀️").Should().HaveCount(1);
            }

            File.Delete(fileName);
        }

        private static async Task<FullTextIndex<string>> SearializeAndDeserializeIndexWithText(string text)
        {
            var stream = new MemoryStream();
            var serializer = new BinarySerializer<string>();
            var index = new FullTextIndexBuilder<string>().Build();
            await index.AddAsync("A", text);

            await serializer.SerializeAsync(index, stream, false);

            stream.Position = 0;

            var index2 = new FullTextIndexBuilder<string>().Build();
            await serializer.DeserializeAsync(index2, stream);
            return index2;
        }

        private async Task<FullTextIndex<string>> CreateWikipediaIndexAsync()
        {
            var index = new FullTextIndexBuilder<string>()
                .WithTextExtractor<XmlTextExtractor>()
                .WithDefaultTokenization(o => o.WithStemming())
                .Build();

            var wikipediaTests = WikipediaDataLoader.Load(typeof(FullTextIndexTests));
            foreach (var (name, text) in wikipediaTests)
            {
                await index.AddAsync(name, text);
            }

            // For good measure, index some surrogate pairs
            await index.AddAsync("Emoji", "Emojis can cause problems 🤷‍♀️ 🤷🏾‍♂️");

            return index;
        }
    }
}

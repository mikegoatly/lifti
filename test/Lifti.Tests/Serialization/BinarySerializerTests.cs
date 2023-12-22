using FluentAssertions;
using Lifti.Serialization.Binary;
using Lifti.Tokenization.TextExtraction;
using PerformanceProfiling;
using System;
using System.Collections.Generic;
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
            var index = await SerializeAndDeserializeIndexWithTextAsync("🎶 🤷🏾‍♀️");
            index.Search("🤷🏾‍♀️").Should().HaveCount(1);
        }

        [Fact]
        public async Task ShouldSerializeEmoji()
        {
            var index = await SerializeAndDeserializeIndexWithTextAsync("🎶");
            index.Search("🎶").Should().HaveCount(1);
        }

        [Fact]
        public async Task ShouldSerializeEmojiSequences()
        {
            var index = await SerializeAndDeserializeIndexWithTextAsync("🎶🤷🏾‍♀️");
            index.Search("🎶🤷🏾‍♀️").Should().HaveCount(1);
        }

        [Fact]
        public async Task SerializationShouldPreserveDynamicFieldInformation()
        {
            var indexBuilder = new FullTextIndexBuilder<int>()
                .WithObjectTokenization<DynamicFieldObject>(
                    cfg => cfg
                        .WithKey(x => x.Id)
                        .WithDynamicFields("DynFields", x => x.Fields));

            var index = indexBuilder.Build();

            await index.AddAsync(new DynamicFieldObject
            {
                Id = 1,
                Fields = new Dictionary<string, string>
                {
                    { "Foo", "Just some test text" },
                    { "Bar", "More stuff to search" }
                }
            });

            var deserializedIndex = indexBuilder.Build();
            await SerializeAndDeserializeAsync(index, deserializedIndex);

            deserializedIndex.Search("Foo=test").Should().HaveCount(1);
        }

        [Fact]
        public async Task WhenNewStaticFieldsIntroduced_FieldIdsShouldBeMapped()
        {
            var index = new FullTextIndexBuilder<int>()
                .WithObjectTokenization<DynamicFieldObject>(
                    cfg => cfg
                        .WithKey(x => x.Id)
                        .WithDynamicFields("DynFields", x => x.Fields))
                .Build();

            await index.AddAsync(new DynamicFieldObject
            {
                Id = 1,
                Fields = new Dictionary<string, string>
                {
                    { "Foo", "Just some test text" },
                    { "Bar", "More stuff to search" }
                }
            });

            index.FieldLookup.GetFieldInfo("Foo").Id.Should().Be(1);

            var deserializedIndex = new FullTextIndexBuilder<int>()
                .WithObjectTokenization<DynamicFieldObject>(
                    cfg => cfg
                        .WithKey(x => x.Id)
                        .WithField("Name", x => x.Name)
                        .WithDynamicFields("DynFields", x => x.Fields))
                .Build();

            await SerializeAndDeserializeAsync(index, deserializedIndex);

            deserializedIndex.FieldLookup.AllFieldNames.Should().BeEquivalentTo(
                new[]
                {
                    "Name",
                    "Foo",
                    "Bar"
                });

            deserializedIndex.FieldLookup.GetFieldInfo("Foo").Id.Should().Be(2);

            deserializedIndex.Search("Foo=test").Should().HaveCount(1);
        }

        [Fact]
        public async Task DeserializingIndexWhenStaticFieldRemoved_ShouldThrowException()
        {
            var index = new FullTextIndexBuilder<int>()
                .WithObjectTokenization<DynamicFieldObject>(
                    cfg => cfg
                        .WithKey(x => x.Id)
                        .WithField("Name", x => x.Name)
                        .WithDynamicFields("DynFields", x => x.Fields))
                .Build();

            await index.AddAsync(new DynamicFieldObject
            {
                Id = 1,
                Name = "Blah",
                Fields = new Dictionary<string, string>
                {
                    { "Foo", "Just some test text" },
                    { "Bar", "More stuff to search" }
                }
            });

            var deserializedIndex = new FullTextIndexBuilder<int>()
                .WithObjectTokenization<DynamicFieldObject>(
                    cfg => cfg
                        .WithKey(x => x.Id)
                        .WithDynamicFields("DynFields", x => x.Fields))
                .Build();

            var exception = await Assert.ThrowsAsync<LiftiException>(async () => await SerializeAndDeserializeAsync(index, deserializedIndex));

            exception.Message.Should().Be("Unknown field 'Name'");
        }

        [Fact]
        public async Task DeserializingIndexWhenDynamicFieldReaderRemoved_ShouldThrowException()
        {
            var index = new FullTextIndexBuilder<int>()
                .WithObjectTokenization<DynamicFieldObject>(
                    cfg => cfg
                        .WithKey(x => x.Id)
                        .WithField("Name", x => x.Name)
                        .WithDynamicFields("DynFields", x => x.Fields))
                .Build();

            await index.AddAsync(new DynamicFieldObject
            {
                Id = 1,
                Name = "Blah",
                Fields = new Dictionary<string, string>
                {
                    { "Foo", "Just some test text" },
                    { "Bar", "More stuff to search" }
                }
            });

            var deserializedIndex = new FullTextIndexBuilder<int>()
                .WithObjectTokenization<DynamicFieldObject>(
                    cfg => cfg
                        .WithKey(x => x.Id)
                        .WithField("Name", x => x.Name))
                .Build();

            var exception = await Assert.ThrowsAsync<LiftiException>(async () => await SerializeAndDeserializeAsync(index, deserializedIndex));

            exception.Message.Should().Be("An unknown dynamic field reader name was encountered: DynFields - this would likely indicate that an index was serialized with a differently configured set of dynamic field readers.");
        }

        [Fact]
        public async Task ShouldDeserializeV6Index()
        {
            var index = CreateObjectIndex();

            var serializer = new BinarySerializer<int>();
            using (var stream = new MemoryStream(TestResources.v6Index))
            {
                await serializer.DeserializeAsync(index, stream);
            }

            index.Search("serialized").Should().HaveCount(1);
            index.Search("亜").Should().HaveCount(1);

            var objectScoreBoostMetadata = index.Items.GetObjectTypeScoreBoostMetadata(1);
            // The first item in the index wins for both score boosts, each with a weighting of 10.
            objectScoreBoostMetadata.CalculateScoreBoost(index.Items.GetMetadata(0))
                .Should().Be(20D);

            // The first item in the index is at the bottom end of both scales, so should return 2.
            objectScoreBoostMetadata.CalculateScoreBoost(index.Items.GetMetadata(1))
                .Should().Be(2D);
        }

        [Fact]
        public async Task ShouldDeserializeV5Index()
        {
            var index = CreateObjectIndex();

            var serializer = new BinarySerializer<int>();
            using (var stream = new MemoryStream(TestResources.v5Index))
            {
                await serializer.DeserializeAsync(index, stream);
            }

            index.Search("serialized").Should().HaveCount(1);
            index.Search("亜").Should().HaveCount(1);

            // The old index won't have any scoring data associated to the documents, so should return 1
            index.Items.GetObjectTypeScoreBoostMetadata(1).CalculateScoreBoost(index.Items.GetMetadata(0))
                .Should().Be(1D);
        }

        [Fact]
        public async Task DeserializingV4IndexWithRemovedField_ShouldThrowException()
        {
            var index = new FullTextIndexBuilder<int>()
                 .WithObjectTokenization<DynamicFieldObject>(
                      cfg => cfg
                          .WithKey(x => x.Id)
                          .WithField("Name", x => x.Name))
                .Build();

            var serializer = new BinarySerializer<int>();
            using var stream = new MemoryStream(TestResources.v4Index);
            var exception = await Assert.ThrowsAsync<LiftiException>(async () => await serializer.DeserializeAsync(index, stream));

            exception.Message.Should().Be("Serialized index contains unknown field ids. Fields have most likely been removed from the FullTextIndexBuilder configuration.");
        }

        [Fact]
        public async Task ShouldDeserializeV4Index()
        {
            var index = new FullTextIndexBuilder<int>()
                 .WithObjectTokenization<DynamicFieldObject>(
                      cfg => cfg
                          .WithKey(x => x.Id)
                          .WithField("Name", x => x.Name)
                          .WithField("SomethingElse", x => x.SomethingElse))
                .Build();

            var serializer = new BinarySerializer<int>();
            using (var stream = new MemoryStream(TestResources.v4Index))
            {
                await serializer.DeserializeAsync(index, stream);
            }

            index.Search("blah").Should().HaveCount(1);
            index.Search("cheese").Should().HaveCount(1);
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
            var fileName = CreateRandomIndexFileName();

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

        [Fact]
        public async Task ShouldBeAbleToSerializeAndDeserializeMultipleIndexesToTheSameStream()
        {
            var index1 = await CreateIndexAsync("Foo");
            var index2 = await CreateIndexAsync("Bar");
            var fileName = CreateRandomIndexFileName();

            var serializer = new BinarySerializer<string>();
            using (var stream = File.Open(fileName, FileMode.CreateNew))
            {
                await serializer.SerializeAsync(index1, stream, false);
                await serializer.SerializeAsync(index2, stream, true);
            }

            using (var stream = File.Open(fileName, FileMode.Open))
            {
                var deserializedIndex1 = new FullTextIndexBuilder<string>().Build();
                var deserializedIndex2 = new FullTextIndexBuilder<string>().Build();
                await serializer.DeserializeAsync(deserializedIndex1, stream, false);
                await serializer.DeserializeAsync(deserializedIndex2, stream, true);

                deserializedIndex1.Search("Foo").Should().HaveCount(1);
                deserializedIndex2.Search("Bar").Should().HaveCount(1);
            }
        }

        [Fact]
        public async Task ShouldRoundTripMixedScoringMetadata()
        {
            var index = CreateObjectIndex();

            // One object with score boost info
            await index.AddAsync(new DynamicFieldObject
            {
                Id = 1,
                CreatedDate = new DateTime(2022, 10, 1),
                Importance = 5D,
                Name = "Blah",
                SomethingElse = "Great",
                Fields = new Dictionary<string, string>
                {
                    { "Foo", "Some serialized data" },
                    { "Bar", "More text" }
                }
            });

            // One object without score boost info
            await index.AddAsync(new DynamicFieldObject
            {
                Id = 2,
                Name = "Cheese",
                SomethingElse = "Great",
                Fields = new Dictionary<string, string>
                {
                        { "Foo", "Other data" },
                        { "Bar", "亜" }
                    }
            });

            // One piece of loose text
            await index.AddAsync(3, "Loose text");

            var deserialized = CreateObjectIndex();
            await SerializeAndDeserializeAsync(index, deserialized);

            var metadata = deserialized.Items.GetMetadata(0);
            metadata.ScoringFreshnessDate.Should().Be(new DateTime(2022, 10, 1));
            metadata.ScoringMagnitude.Should().Be(5D);

            metadata = deserialized.Items.GetMetadata(1);
            metadata.ScoringFreshnessDate.Should().BeNull();
            metadata.ScoringMagnitude.Should().BeNull();

            metadata = deserialized.Items.GetMetadata(2);
            metadata.ScoringFreshnessDate.Should().BeNull();
            metadata.ScoringMagnitude.Should().BeNull();
        }

        // Used to create test indexes when defining a new serialization version
        //[Fact]
        //public async Task CreateTestIndex()
        //{
        //    var index = CreateObjectIndex();

        //    await index.AddAsync(new DynamicFieldObject
        //    {
        //        Id = 1,
        //        CreatedDate = new DateTime(2022, 10, 1),
        //        Importance = 5D,
        //        Name = "Blah",
        //        SomethingElse = "Great",
        //        Fields = new Dictionary<string, string>
        //        {
        //            { "Foo", "Some serialized data" },
        //            { "Bar", "More text" }
        //        }
        //    });

        //    await index.AddAsync(new DynamicFieldObject
        //    {
        //        Id = 2,
        //        CreatedDate = new DateTime(2021, 10, 1),
        //        Importance = 2D,
        //        Name = "Cheese",
        //        SomethingElse = "Great",
        //        Fields = new Dictionary<string, string>
        //        {
        //            { "Foo", "Other data" },
        //            { "Bar", "亜" }
        //        }
        //    });

        //    var serializer = new BinarySerializer<int>();
        //    using var stream = File.Open("../../../V6.dat", FileMode.Create);
        //    await serializer.SerializeAsync(index, stream, true);
        //}

        private static FullTextIndex<int> CreateObjectIndex()
        {
            return new FullTextIndexBuilder<int>()
                .WithObjectTokenization<DynamicFieldObject>(
                    cfg => cfg
                        .WithKey(x => x.Id)
                        .WithField("Name", x => x.Name)
                        .WithField("SomethingElse", x => x.SomethingElse)
                        .WithDynamicFields("DynFields", x => x.Fields)
                        .WithScoreBoosting(x => x
                            .Freshness(o => o.CreatedDate, 10)
                            .Magnitude(o => o.Importance, 10)))
                .Build();
        }

        private static string CreateRandomIndexFileName()
        {
            return Guid.NewGuid().ToString() + ".dat";
        }

        private static async Task<FullTextIndex<string>> SerializeAndDeserializeIndexWithTextAsync(string text)
        {
            var stream = new MemoryStream();
            var serializer = new BinarySerializer<string>();
            var index = await CreateIndexAsync(text);

            await serializer.SerializeAsync(index, stream, false);

            stream.Position = 0;

            var index2 = new FullTextIndexBuilder<string>().Build();
            await serializer.DeserializeAsync(index2, stream);
            return index2;
        }

        private static async Task<FullTextIndex<string>> CreateIndexAsync(string text)
        {
            var index = new FullTextIndexBuilder<string>().Build();
            await index.AddAsync("A", text);
            return index;
        }

        private static async Task<FullTextIndex<string>> CreateWikipediaIndexAsync()
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

        private static async Task SerializeAndDeserializeAsync(FullTextIndex<int> index, FullTextIndex<int> deserializedIndex)
        {
            var serializer = new BinarySerializer<int>();
            using var stream = new MemoryStream();
            await serializer.SerializeAsync(index, stream, false);
            stream.Position = 0;
            await serializer.DeserializeAsync(deserializedIndex, stream);
        }

        private class DynamicFieldObject
        {
            public int Id { get; set; }
            public string Name { get; set; } = null!;
            public string SomethingElse { get; set; } = null!;
            public Dictionary<string, string>? Fields { get; set; }
            public DateTime? CreatedDate { get; internal set; }
            public double? Importance { get; internal set; }
        }
    }
}

using FluentAssertions;
using Lifti.Serialization.Binary;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace Lifti.Tests.Serialization
{
    public class BinarySerializerTests
    {
        [Fact]
        public async Task ShouldRoundTripIndexStructure()
        {
            var index = new FullTextIndex<string>();
            index.Index("A", "This is a test string for serialization");
            index.Index("B", "Also a test string and should be serialized");

            var serializer = new BinarySerializer<string>();

            using (var stream = new MemoryStream())
            {
                await serializer.SerializeAsync(index, stream, false);

                stream.Length.Should().BeGreaterThan(4);

                var newIndex = new FullTextIndex<string>();

                stream.Position = 0;
                await serializer.DeserializeAsync(newIndex, stream, false);

                newIndex.IdPool.GetIndexedItems().Should().BeEquivalentTo(index.IdPool.GetIndexedItems());
                newIndex.Count.Should().Be(index.Count);
                newIndex.Root.ToString().Should().Be(index.Root.ToString());

                newIndex.Search("seria*").Should().BeEquivalentTo(
                    new SearchResult<string>(
                        "A",
                        new[] { new FieldSearchResult("Unspecified", new[] { new WordLocation(6, 26, 13) }) }),
                    new SearchResult<string>(
                        "B",
                        new[] { new FieldSearchResult("Unspecified", new[] { new WordLocation(7, 33, 10) }) }));
            }
        }
    }
}

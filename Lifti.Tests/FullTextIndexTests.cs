using FluentAssertions;
using PerformanceProfiling;
using Xunit;

namespace Lifti.Tests
{
    public class FullTextIndexTests
    {
        private readonly FullTextIndex<string> index;

        public FullTextIndexTests()
        {
            this.index = new FullTextIndex<string>(
                new FullTextIndexOptions<string>
                {
                    Advanced = { SupportIntraNodeTextAfterCharacterIndex = 4 }
                });
        }

        [Fact]
        public void IndexedItemsShouldBeRetrievable()
        {
            this.index.Index("A", "This is a test");
            this.index.Index("B", "This is another test");
            this.index.Index("C", "Foo is testing this as well");
            this.index.Index("D", "One last test just for testing sake");

            var results = this.index.Search("this");

            results.Should().HaveCount(3);
        }

        [Fact]
        public void WhenLoadingLotsOfDataShouldNotError()
        {
            foreach (var entry in WikipediaDataLoader.Load(this.GetType()))
            {
                this.index.Index(entry.name, entry.text);
            }
        }
    }
}

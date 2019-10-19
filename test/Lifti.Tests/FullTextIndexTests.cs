using FluentAssertions;
using PerformanceProfiling;
using System.Linq;
using Xunit;

namespace Lifti.Tests
{
    public class FullTextIndexTests
    {
        private readonly FullTextIndex<string> index;

        public FullTextIndexTests()
        {
            this.index = new FullTextIndex<string>(
                new FullTextIndexConfiguration<string>
                {
                    Advanced = { SupportIntraNodeTextAfterCharacterIndex = 4 }
                });

            this.index.WithItemTokenization<TestObject>(o => o.Id)
                .WithField("Text1", o => o.Text1)
                .WithField("Text2", o => o.Text2)
                .WithField("Text3", o => o.Text3);
        }

        [Fact]
        public void IndexedItemsShouldBeRetrievable()
        {
            this.WithIndexedStrings();

            var results = this.index.Search("this test");

            results.Should().HaveCount(2);
        }

        [Fact]
        public void WordsRetrievedBySearchingForTextIndexedBySimpleStringsShouldBeAssociatedToDefaultField()
        {
            this.WithIndexedStrings();

            var results = this.index.Search("this");

            results.All(i => i.FieldMatches.All(l => l.FoundIn == "Unspecified")).Should().BeTrue();
        }

        [Fact]
        public void SearchingByMultipleWildcards_ShouldReturnResult()
        {
            this.WithIndexedStrings();

            var results = this.index.Search("fo* te*");

            results.Should().HaveCount(2);
        }

        [Fact]
        public void IndexedObjectsShouldBeRetrievableByTextFromAnyIndexedField()
        {
            this.WithIndexedObjects();

            this.index.Search("text").Should().HaveCount(1);
            this.index.Search("one").Should().HaveCount(2);
            this.index.Search("two").Should().HaveCount(2);
            this.index.Search("three").Should().HaveCount(2);
        }

        [Fact]
        public void WordsRetrievedBySearchingForTextIndexedByObjectsShouldBeAssociatedToCorrectFields()
        {
            this.WithIndexedObjects();

            var results = this.index.Search("one");
            results.All(i => i.FieldMatches.All(l => l.FoundIn == "Text1")).Should().BeTrue();
            results = this.index.Search("two");
            results.All(i => i.FieldMatches.All(l => l.FoundIn == "Text2")).Should().BeTrue();
            results = this.index.Search("three");
            results.All(i => i.FieldMatches.All(l => l.FoundIn == "Text3")).Should().BeTrue();
        }

        [Fact]
        public void RemovingItemFromIndex_ShouldMakeItUnsearchable()
        {
            this.WithIndexedStrings();

            this.index.Search("foo").Should().HaveCount(1);

            this.index.Remove("C").Should().BeTrue();

            this.index.Search("foo").Should().HaveCount(0);
        }

        [Fact]
        public void RemovingItemFromIndexThatIsntIndexed_ShouldReturnFalse()
        {
            this.WithIndexedStrings();

            this.index.Remove("Z").Should().BeFalse();
            this.index.Remove("C").Should().BeTrue();
            this.index.Remove("C").Should().BeFalse();
        }

        [Fact]
        public void WhenLoadingLotsOfDataShouldNotError()
        {
            var wikipediaTests = WikipediaDataLoader.Load(this.GetType());
            foreach (var (name, text) in wikipediaTests)
            {
                this.index.Index(name, text);
            }

            this.index.Remove(wikipediaTests[10].name);
            this.index.Remove(wikipediaTests[9].name);
            this.index.Remove(wikipediaTests[8].name);
        }

        private void WithIndexedObjects()
        {
            this.index.Index(new TestObject("A", "Text One", "Text Two", "Text Three"));
            this.index.Index(new TestObject("B", "Not One", "Not Two", "Not Three"));
        }

        private void WithIndexedStrings()
        {
            this.index.Index("A", "This is a test");
            this.index.Index("B", "This is another test");
            this.index.Index("C", "Foo is testing this as well");
            this.index.Index("D", "One last test just for testing sake");
        }

        public class TestObject
        {
            public TestObject(string id, string text1, string text2, string text3)
            {
                this.Id = id;
                this.Text1 = text1;
                this.Text2 = text2;
                this.Text3 = text3;
            }

            public string Id { get; }
            public string Text1 { get; }
            public string Text2 { get; }
            public string Text3 { get; }
        }
    }
}

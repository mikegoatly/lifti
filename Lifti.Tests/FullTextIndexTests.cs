using FluentAssertions;
using PerformanceProfiling;
using System.Linq;
using Xunit;

namespace Lifti.Tests
{
    public class FullTextIndexTests
    {
        private readonly FullTextIndex<string> index;

        private readonly ItemTokenizationOptions<TestObject, string> tokenizationOptions = new ItemTokenizationOptions<TestObject, string>(
                o => o.Id,
                new FieldTokenizationOptions<TestObject>("Text1", o => o.Text1),
                new FieldTokenizationOptions<TestObject>("Text2", o => o.Text2),
                new FieldTokenizationOptions<TestObject>("Text3", o => o.Text3));

        public FullTextIndexTests()
        {
            this.index = new FullTextIndex<string>(
                new FullTextIndexConfiguration<string>
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

            var results = this.index.Search("this test");

            results.Should().HaveCount(2);
        }

        [Fact]
        public void WordsRetrievedBySearchingForTextIndexedBySimpleStringsShouldBeAssociatedToDefaultField()
        {
            this.index.Index("A", "This is a test");
            this.index.Index("B", "This is another test");

            var results = this.index.Search("this");

            results.All(i => i.Locations.All(l => l.FoundIn == "Unspecified")).Should().BeTrue();
        }

        [Fact]
        public void IndexedObjectsShouldBeRetrievableByTextFromAnyIndexedField()
        {
            this.index.Index(new TestObject("A", "Text One", "Text Two", "Text Three"), this.tokenizationOptions);
            this.index.Index(new TestObject("B", "Not One", "Not Two", "Not Three"), this.tokenizationOptions);

            this.index.Search("text").Should().HaveCount(1);
            this.index.Search("one").Should().HaveCount(2);
            this.index.Search("two").Should().HaveCount(2);
            this.index.Search("three").Should().HaveCount(2);
        }

        [Fact]
        public void WordsRetrievedBySearchingForTextIndexedByObjectsShouldBeAssociatedToCorrectFields()
        {
            this.index.Index(new TestObject("A", "Text One", "Text Two", "Text Three"), this.tokenizationOptions);
            this.index.Index(new TestObject("B", "Not One", "Not Two", "Not Three"), this.tokenizationOptions);

            var results = this.index.Search("one");
            results.All(i => i.Locations.All(l => l.FoundIn == "Text1")).Should().BeTrue();
            results = this.index.Search("two");
            results.All(i => i.Locations.All(l => l.FoundIn == "Text2")).Should().BeTrue();
            results = this.index.Search("three");
            results.All(i => i.Locations.All(l => l.FoundIn == "Text3")).Should().BeTrue();
        }

        [Fact]
        public void WhenLoadingLotsOfDataShouldNotError()
        {
            foreach (var entry in WikipediaDataLoader.Load(this.GetType()))
            {
                this.index.Index(entry.name, entry.text);
            }
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

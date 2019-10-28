using FluentAssertions;
using PerformanceProfiling;
using System.Linq;
using Xunit;

namespace Lifti.Tests
{
    public class FullTextIndexTests
    {
        private readonly IFullTextIndex<string> index;

        public FullTextIndexTests()
        {
            this.index = new FullTextIndexBuilder<string>()
                .WithItemTokenization<TestObject>(
                    o => o.WithKey(i => i.Id)
                        .WithField("Text1", i => i.Text1, opts => opts.CaseInsensitive(false))
                        .WithField("Text2", i => i.Text2)
                        .WithField("Text3", i => i.Text3, opts => opts.WithStemming()))
                .WithItemTokenization<TestObject2>(
                    o => o.WithKey(i => i.Id)
                        .WithField("MultiText", i => i.Text))
                .Build();
        }

        [Fact]
        public void IndexedItemsShouldBeRetrievable()
        {
            this.WithIndexedStrings();

            var results = this.index.Search("this test");

            results.Should().HaveCount(2);
        }

        [Fact]
        public void IndexShouldBeSearchableWithHypenatedText()
        {
            this.WithIndexedStrings();

            var results = this.index.Search("third-eye");

            results.Should().HaveCount(1);
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
        public void IndexingItemsShouldObeyTokenizationOptionsForFields()
        {
            this.WithIndexedSingleStringPropertyObjects();

            var options = new TokenizationOptionsBuilder().CaseInsensitive(false).Build();
            this.index.Search("one", options).Should().HaveCount(0);
            this.index.Search("One", options).Should().HaveCount(2);

            options = new TokenizationOptionsBuilder().WithStemming().Build();
            this.index.Search("summer", options).Should().HaveCount(1);
            this.index.Search("summers", options).Should().HaveCount(1);
            this.index.Search("drum", options).Should().HaveCount(1);
            this.index.Search("drumming", options).Should().HaveCount(1);
            this.index.Search("drums", options).Should().HaveCount(1);
        }

        [Fact]
        public void IndexedObjectsShouldBeRetrievableByTextFromAnyIndexedField()
        {
            this.WithIndexedSingleStringPropertyObjects();

            this.index.Search("two").Should().HaveCount(2);
            this.index.Search("three").Should().HaveCount(2);
        }

        [Fact]
        public void WordsRetrievedBySearchingForTextIndexedByObjectsShouldBeAssociatedToCorrectFields()
        {
            this.WithIndexedSingleStringPropertyObjects();

            var results = this.index.Search("one");
            results.All(i => i.FieldMatches.All(l => l.FoundIn == "Text1")).Should().BeTrue();
            results = this.index.Search("two");
            results.All(i => i.FieldMatches.All(l => l.FoundIn == "Text2")).Should().BeTrue();
            results = this.index.Search("three");
            results.All(i => i.FieldMatches.All(l => l.FoundIn == "Text3")).Should().BeTrue();
        }

        [Fact]
        public void IndexedMultiStringPropertyObjectsShouldBeRetrievableByTextFromAnyIndexedField()
        {
            this.WithIndexedMultiStringPropertyObjects();

            this.index.Search("text").Should().HaveCount(1);
            this.index.Search("one").Should().HaveCount(2);
            this.index.Search("two").Should().HaveCount(2);
            this.index.Search("three").Should().HaveCount(2);
        }

        [Fact]
        public void WordsRetrievedBySearchingForTextIndexedByMultiStringPropertyObjectsShouldBeAssociatedToCorrectFields()
        {
            this.WithIndexedMultiStringPropertyObjects();

            var results = this.index.Search("one");
            results.All(i => i.FieldMatches.All(l => l.FoundIn == "MultiText")).Should().BeTrue();
            results = this.index.Search("two");
            results.All(i => i.FieldMatches.All(l => l.FoundIn == "MultiText")).Should().BeTrue();
            results = this.index.Search("three");
            results.All(i => i.FieldMatches.All(l => l.FoundIn == "MultiText")).Should().BeTrue();
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
                this.index.Add(name, text);
            }

            this.index.Remove(wikipediaTests[10].name);
            this.index.Remove(wikipediaTests[9].name);
            this.index.Remove(wikipediaTests[8].name);
        }

        private void WithIndexedSingleStringPropertyObjects()
        {
            this.index.Add(new TestObject("A", "Text One", "Text Two", "Text Three Drumming"));
            this.index.Add(new TestObject("B", "Not One", "Not Two", "Not Three Summers"));
        }

        private void WithIndexedMultiStringPropertyObjects()
        {
            this.index.Add(new TestObject2("A", "Text One", "Text Two", "Text Three"));
            this.index.Add(new TestObject2("B", "Not One", "Not Two", "Not Three"));
        }

        private void WithIndexedStrings()
        {
            this.index.Add("A", "This is a test");
            this.index.Add("B", "This is another test");
            this.index.Add("C", "Foo is testing this as well");
            this.index.Add("D", "One last test just for testing sake with hypenated text: third-eye");
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

        public class TestObject2
        {
            public TestObject2(string id, params string[] text)
            {
                this.Id = id;
                this.Text = text;
            }

            public string Id { get; }
            public string[] Text { get; }
        }
    }
}

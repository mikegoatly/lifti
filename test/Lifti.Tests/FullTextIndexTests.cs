using FluentAssertions;
using PerformanceProfiling;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Lifti.Tests
{
    public class FullTextIndexTests
    {
        private readonly FullTextIndex<string> index;

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
                .WithItemTokenization<TestObject3>(
                    o => o.WithKey(i => i.Id)
                        .WithField("TextAsync", i => Task.Run(() => i.Text))
                        .WithField("MultiTextAsync", i => Task.Run(() => (IEnumerable<string>)i.MultiText)))
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
        public void SearchingWithinFieldsShouldObeyTokenizationOptionsForFields()
        {
            this.WithIndexedSingleStringPropertyObjects();

            this.index.Search("Text1=one").Should().HaveCount(0);
            this.index.Search("Text1=One").Should().HaveCount(2);

            this.index.Search("Text3=summer").Should().HaveCount(1);
            this.index.Search("Text3=summers").Should().HaveCount(1);
            this.index.Search("Text3=drum").Should().HaveCount(1);
            this.index.Search("Text3=drumming").Should().HaveCount(1);
            this.index.Search("Text3=drums").Should().HaveCount(1);
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
        public async Task IndexedAsyncFieldsShouldBeRetrievableByTextFromAnyIndexedField()
        {
            await this.WithIndexedAsyncFieldObjectsAsync();

            this.index.Search("text").Should().HaveCount(1);
            this.index.Search("one").Should().HaveCount(2);
            this.index.Search("two").Should().HaveCount(2);
            this.index.Search("three").Should().HaveCount(2);
        }

        [Fact]
        public void IndexingFieldWithoutAsyncWhenAsyncRequired_ShouldThrowException()
        {
            Assert.Throws<LiftiException>(() => this.index.Add(new TestObject3("1", "!", "11")));
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
        public void RemovingLastItemFromIndex_ShouldReturnTrue()
        {
            this.index.Add("A", "foo");

            this.index.Search("foo").Should().HaveCount(1);

            this.index.Remove("A").Should().BeTrue();

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

        [Fact]
        public async Task WritingToIndexFromMultipleThreadsSimultaneously_ShouldUpdateIndexCorrectly()
        {
            var startBarrier = new Barrier(10);
            void Index(int id)
            {
                startBarrier.SignalAndWait();
                this.index.Add(id.ToString(), "Test testing another test");
            }

            var tasks = new List<Task>();
            for (var i = 0; i < 10; i++)
            {
                var id = i;
                tasks.Add(Task.Run(() => Index(id)));
            }

            await Task.WhenAll(tasks);

            this.index.Count.Should().Be(10);
            this.index.Search("test").Should().HaveCount(10);
        }

        [Fact]
        public async Task WritingToIndexInBatch_WritesShouldOnlyBeReadableOnceBatchCompletes()
        {
            this.index.BeginBatchChange();

            this.WithIndexedSingleStringPropertyObjects();

            this.index.Count.Should().Be(0);
            this.index.Search("three").Count().Should().Be(0);

            await this.index.CommitBatchChangeAsync();

            this.index.Count.Should().Be(2);
            this.index.Search("three").Count().Should().Be(2);
        }

        [Fact]
        public async Task AddingToAndRemovingTheSameItemInOneBatch_ShouldResultInItemNotIndexed()
        {
            this.index.BeginBatchChange();

            this.index.Add("A", "Test");
            this.index.Add("B", "Test");
            this.index.Remove("A");

            await this.index.CommitBatchChangeAsync();

            this.index.Count.Should().Be(1);
            this.index.Search("test").Select(t => t.Key).Should().BeEquivalentTo("B");
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

        private async Task WithIndexedAsyncFieldObjectsAsync()
        {
            await this.index.AddAsync(new TestObject3("A", "Text One", "Text Two", "Text Three"));
            await this.index.AddAsync(new TestObject3("B", "Not One", "Not Two", "Not Three"));
        }

        private void WithIndexedStrings()
        {
            this.index.Add("A", "This is a test");
            this.index.Add("B", "This is another test");
            this.index.Add("C", "Foo is testing this as well");
            this.index.Add("D", new[] { "One last test just for testing sake", "with hypenated text: third-eye" });
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

        public class TestObject3
        {
            public TestObject3(string id, string text, params string[] multiText)
            {
                this.Id = id;
                this.Text = text;
                this.MultiText = multiText;
            }

            public string Id { get; }
            public string Text { get; }
            public string[] MultiText { get; }
        }
    }
}

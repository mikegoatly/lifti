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
        public async Task IndexingItemsAgainstDefaultField_ShouldUpdateTotalTokenCountStats()
        {
            await this.WithIndexedStringsAsync();

            this.index.Items.IndexStatistics.TotalTokenCount.Should().Be(26);
            this.index.Items.IndexStatistics.TokenCountByField.Should().BeEquivalentTo(new Dictionary<byte, long>
            {
                { 0, 26 }
            });
        }

        [Fact]
        public async Task IndexingItemsAgainstWithMultipleFields_ShouldUpdateTotalTokenCountStats()
        {
            await this.WithIndexedSingleStringPropertyObjectsAsync();

            this.index.Items.IndexStatistics.TotalTokenCount.Should().Be(14);
            this.index.Items.IndexStatistics.TokenCountByField.Should().BeEquivalentTo(new Dictionary<byte, long>
            {
                { 1, 4 },
                { 2, 4 },
                { 3, 6 }
            });
        }

        [Fact]
        public async Task IndexedItemsShouldBeRetrievable()
        {
            await this.WithIndexedStringsAsync();

            var results = this.index.Search("this test");

            results.Should().HaveCount(2);
        }

        [Fact]
        public async Task IndexShouldBeSearchableWithHypenatedText()
        {
            await this.WithIndexedStringsAsync();

            var results = this.index.Search("third-eye");

            results.Should().HaveCount(1);
        }

        [Fact]
        public async Task WordsRetrievedBySearchingForTextIndexedBySimpleStringsShouldBeAssociatedToDefaultField()
        {
            await this.WithIndexedStringsAsync();

            var results = this.index.Search("this");

            results.All(i => i.FieldMatches.All(l => l.FoundIn == "Unspecified")).Should().BeTrue();
        }

        [Fact]
        public async Task SearchingByMultipleWildcards_ShouldReturnResult()
        {
            await this.WithIndexedStringsAsync();

            var results = this.index.Search("fo* te*");

            results.Should().HaveCount(2);
        }

        [Fact]
        public async Task SearchingForEmptyString_ShouldReturnEmptyResults()
        {
            await this.WithIndexedStringsAsync();

            var results = this.index.Search("");

            results.Should().HaveCount(0);
        }

        [Fact]
        public async Task ReindexingItem_ShouldReplaceIndexedTextInIndex()
        {
            await this.index.AddAsync("A", "Test");
            await this.index.AddAsync("A", "Replaced");

            this.index.Search("Test").Should().HaveCount(0);
            this.index.Search("Replaced").Should().HaveCount(1);
        }

        [Fact]
        public async Task SearchingWithinFieldsShouldObeyTokenizationOptionsForFields()
        {
            await this.WithIndexedSingleStringPropertyObjectsAsync();

            this.index.Search("Text1=one").Should().HaveCount(0);
            this.index.Search("Text1=One").Should().HaveCount(2);

            this.index.Search("Text3=summer").Should().HaveCount(1);
            this.index.Search("Text3=summers").Should().HaveCount(1);
            this.index.Search("Text3=drum").Should().HaveCount(1);
            this.index.Search("Text3=drumming").Should().HaveCount(1);
            this.index.Search("Text3=drums").Should().HaveCount(1);
        }

        [Fact]
        public async Task IndexedObjectsShouldBeRetrievableByTextFromAnyIndexedField()
        {
            await this.WithIndexedSingleStringPropertyObjectsAsync();

            this.index.Search("two").Should().HaveCount(2);
            this.index.Search("three").Should().HaveCount(2);
        }

        [Fact]
        public async Task WordsRetrievedBySearchingForTextIndexedByObjectsShouldBeAssociatedToCorrectFields()
        {
            await this.WithIndexedSingleStringPropertyObjectsAsync();

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
        public async Task IndexedMultiStringPropertyObjectsShouldBeRetrievableByTextFromAnyIndexedField()
        {
            await this.WithIndexedMultiStringPropertyObjectsAsync();

            this.index.Search("text").Should().HaveCount(1);
            this.index.Search("one").Should().HaveCount(2);
            this.index.Search("two").Should().HaveCount(2);
            this.index.Search("three").Should().HaveCount(2);
        }

        [Fact]
        public async Task WordsRetrievedBySearchingForTextIndexedByMultiStringPropertyObjectsShouldBeAssociatedToCorrectFields()
        {
            await this.WithIndexedMultiStringPropertyObjectsAsync();

            var results = this.index.Search("one");
            results.All(i => i.FieldMatches.All(l => l.FoundIn == "MultiText")).Should().BeTrue();
            results = this.index.Search("two");
            results.All(i => i.FieldMatches.All(l => l.FoundIn == "MultiText")).Should().BeTrue();
            results = this.index.Search("three");
            results.All(i => i.FieldMatches.All(l => l.FoundIn == "MultiText")).Should().BeTrue();
        }

        [Fact]
        public async Task RemovingItemFromIndex_ShouldMakeItUnsearchable()
        {
            await this.WithIndexedStringsAsync();

            this.index.Search("foo").Should().HaveCount(1);

            (await this.index.RemoveAsync("C")).Should().BeTrue();

            this.index.Search("foo").Should().HaveCount(0);
        }

        [Fact]
        public async Task RemovingLastItemFromIndex_ShouldReturnTrue()
        {
            await this.index.AddAsync("A", "foo");

            this.index.Search("foo").Should().HaveCount(1);

            (await this.index.RemoveAsync("A")).Should().BeTrue();

            this.index.Search("foo").Should().HaveCount(0);
        }

        [Fact]
        public async Task RemovingItemFromIndexThatIsntIndexed_ShouldReturnFalse()
        {
            await this.WithIndexedStringsAsync();

            (await this.index.RemoveAsync("Z")).Should().BeFalse();
            (await this.index.RemoveAsync("C")).Should().BeTrue();
            (await this.index.RemoveAsync("C")).Should().BeFalse();
        }

        [Fact]
        public async Task QueringIndex_ShouldOrderResultsByScore()
        {
            await PopulateIndexWithWikipediaData();

            var results = this.index.Search("data").ToList();
            results.Should().BeInDescendingOrder(r => r.Score);
            results.First().Score.Should().BeApproximately(2.4349517D, 0.0001D);
            results.Last().Score.Should().BeApproximately(1.2298017D, 0.0001D);
        }

        [Fact]
        public async Task WhenLoadingLotsOfDataShouldNotError()
        {
            var wikipediaTests = await PopulateIndexWithWikipediaData();

            await this.index.RemoveAsync(wikipediaTests[10].name);
            await this.index.RemoveAsync(wikipediaTests[9].name);
            await this.index.RemoveAsync(wikipediaTests[8].name);
        }

        private async Task<IList<(string name, string text)>> PopulateIndexWithWikipediaData()
        {
            var wikipediaTests = WikipediaDataLoader.Load(this.GetType());
            this.index.BeginBatchChange();
            foreach (var (name, text) in wikipediaTests)
            {
                await this.index.AddAsync(name, text);
            }

            await this.index.CommitBatchChangeAsync();
            return wikipediaTests;
        }

        [Fact]
        public async Task WritingToIndexFromMultipleThreadsSimultaneously_ShouldUpdateIndexCorrectly()
        {
            var startBarrier = new Barrier(10);
            async Task IndexAsync(int id)
            {
                startBarrier.SignalAndWait();
                await this.index.AddAsync(id.ToString(), "Test testing another test");
            }

            var tasks = new List<Task>();
            for (var i = 0; i < 10; i++)
            {
                var id = i;
                tasks.Add(Task.Run(async () => await IndexAsync(id)));
            }

            await Task.WhenAll(tasks);

            this.index.Count.Should().Be(10);
            this.index.Search("test").Should().HaveCount(10);
        }

        [Fact]
        public async Task WritingToIndexInBatch_WritesShouldOnlyBeReadableOnceBatchCompletes()
        {
            this.index.BeginBatchChange();

            await this.WithIndexedSingleStringPropertyObjectsAsync();

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

            await this.index.AddAsync("A", "Test");
            await this.index.AddAsync("B", "Test");
            await this.index.RemoveAsync("A");

            await this.index.CommitBatchChangeAsync();

            this.index.Count.Should().Be(1);
            this.index.Search("test").Select(t => t.Key).Should().BeEquivalentTo("B");
        }

        [Fact]
        public async Task RemovingItemAndAddingAgainItTheSameBatch_ShouldResultInTheSameIndex()
        {
            await this.index.AddAsync("A", "Test");
            var previousRoot = this.index.Root;

            this.index.BeginBatchChange();

            await this.index.RemoveAsync("A");
            await this.index.AddAsync("A", "Test");

            await this.index.CommitBatchChangeAsync();

            this.index.Root.Should().BeEquivalentTo(previousRoot);
        }

        private async Task WithIndexedSingleStringPropertyObjectsAsync()
        {
            await this.index.AddAsync(new TestObject("A", "Text One", "Text Two", "Text Three Drumming"));
            await this.index.AddAsync(new TestObject("B", "Not One", "Not Two", "Not Three Summers"));
        }

        private async Task WithIndexedMultiStringPropertyObjectsAsync()
        {
            await this.index.AddAsync(new TestObject2("A", "Text One", "Text Two", "Text Three"));
            await this.index.AddAsync(new TestObject2("B", "Not One", "Not Two", "Not Three"));
        }

        private async Task WithIndexedAsyncFieldObjectsAsync()
        {
            await this.index.AddAsync(new TestObject3("A", "Text One", "Text Two", "Text Three"));
            await this.index.AddAsync(new TestObject3("B", "Not One", "Not Two", "Not Three"));
        }

        private async Task WithIndexedStringsAsync()
        {
            await this.index.AddAsync("A", "This is a test");
            await this.index.AddAsync("B", "This is another test");
            await this.index.AddAsync("C", "Foo is testing this as well");
            await this.index.AddAsync("D", new[] { "One last test just for testing sake", "with hypenated text: third-eye" });
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

using FluentAssertions;
using Lifti.Tests.Fakes;
using Lifti.Tokenization.TextExtraction;
using PerformanceProfiling;
using System;
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
                .WithObjectTokenization<TestObject>(
                    o => o.WithKey(i => i.Id)
                        .WithField("Text1", i => i.Text1, opts => opts.CaseInsensitive(false))
                        .WithField("Text2", i => i.Text2)
                        .WithField("Text3", i => i.Text3, opts => opts.WithStemming())
                        .WithField("Text4", i => i.Text3, textExtractor: new ReversingTextExtractor()))
                .WithObjectTokenization<TestObject2>(
                    o => o.WithKey(i => i.Id)
                        .WithField("MultiText", i => i.Text))
                .WithObjectTokenization<TestObject3>(
                    o => o.WithKey(i => i.Id)
                        .WithField("TextAsync", i => Task.Run(() => i.Text))
                        .WithField("MultiTextAsync", i => Task.Run(() => (IEnumerable<string>)i.MultiText)))
                .Build();
        }

        [Fact]
        public async Task SequentialWildcardMatches_ShouldResultInCorrectMatchedPhrases()
        {
            var data = new[] {
                (1, "This is some text and seems to some"),
                (2, "This one seems to be the one containing the match"),
                (3, "It seems to me that this one won't work either"),
                (4, "This ought to work too"),
            };

            var index = new FullTextIndexBuilder<int>()
                .Build();

            foreach (var entry in data)
            {
                await index.AddAsync(entry.Item1, entry.Item2);
            }

            index.Search("\"* to  *\"").CreateMatchPhrases(x => data.First(d => d.Item1 == x).Item2)
                .SelectMany(x => x.FieldPhrases.SelectMany(x => x.Phrases))
                .Should()
                .BeEquivalentTo(
                new[]
                {
                    "seems to some",
                    "seems to be",
                    "seems to me",
                    "ought to work"
                });
        }

        [Fact]
        public async Task IndexedEmoji_ShouldBeSearchable()
        {
            await this.index.AddAsync("A", new[] { "🎶" });

            var results = this.index.Search("🎶");

            results.Should().HaveCount(1);
        }

        [Fact]
        public async Task IndexedEmoji_ShouldBeRetrievableAsIndexedTokens()
        {
            await this.index.AddAsync("A", "🎶");

            this.index.Snapshot.CreateNavigator().EnumerateIndexedTokens().Should().BeEquivalentTo("🎶");
        }

        [Fact]
        public async Task IndexingMultipleStringsAgainstOneItem_ShouldStoreLocationsOfWordsCorrectlyAcrossFragments()
        {
            await this.index.AddAsync("A", new[] { "test test", "  test" });

            var results = this.index.Search("test");
            results.Should().HaveCount(1);
            results.Single().FieldMatches.Single().Locations.Should().BeEquivalentTo(
                new[]
                {
                    new TokenLocation(0, 0, 4),
                    new TokenLocation(1, 5, 4),
                    new TokenLocation(2, 11, 4)
                });
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

            this.index.Items.IndexStatistics.TotalTokenCount.Should().Be(22L);
            this.index.Items.IndexStatistics.TokenCountByField.Should().BeEquivalentTo(new Dictionary<byte, long>
            {
                { 1, 4 },
                { 2, 4 },
                { 3, 7 },
                { 4, 7 }
            });
        }

        [Fact]
        public async Task IndexingFieldWithCustomTextExtractor_ShouldOnlyApplyTextExtractorToExpectedField()
        {
            await this.WithIndexedSingleStringPropertyObjectsAsync();

            var results = this.index.Search("TAE");
            results.Should().HaveCount(1);
            results.First().FieldMatches.Single().FoundIn.Should().Be("Text4");

            results = this.index.Search("EAT");
            results.Should().HaveCount(1);
            results.First().FieldMatches.Single().FoundIn.Should().Be("Text3");
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

            results.All(i => i.FieldMatches.All(l => l.FoundIn == IndexedFieldLookup.DefaultFieldName)).Should().BeTrue();
        }

        [Fact]
        public async Task ParsedQuery_ShouldReturnExpectedResults()
        {
            await this.WithIndexedStringsAsync();

            var query = this.index.ParseQuery("fo* te*");
            var results = this.index.Search(query);

            results.Should().HaveCount(2);
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

            this.index.Search("Text2=*t").Should().HaveCount(2); //Text and not
            this.index.Search("Text2=?toxt").Should().HaveCount(1); //Text

            this.index.Search("Text3=eat").Should().HaveCount(1);
            this.index.Search("Text3=eats").Should().HaveCount(1);
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
        public async void ObjectsWithMultipleDynamicFieldsShouldGenerateCorrectPrefixedFieldNames()
        {
            var index = await CreateDynamicObjectTestIndexAsync(true);

            index.FieldLookup.AllFieldNames.Should().BeEquivalentTo(
               new[]
               {
                    "Details",
                    "Dyn1Field1",
                    "Dyn1Field2",
                    "Dyn2Field1",
                    "Dyn2Field2",
                    "Dyn1Field3"
               });
        }

        [Fact]
        public async void SearchesCanBePerformedForDynamicFieldsWithPrefixes()
        {
            var index = await CreateDynamicObjectTestIndexAsync(true);

            var resultsWithoutFieldFilter = index.Search("Three").ToList();
            var resultsWithFieldFilter = index.Search("Dyn1Field3=Three").ToList();

            resultsWithoutFieldFilter.Should().HaveCount(1);

            resultsWithFieldFilter.Should().BeEquivalentTo(resultsWithoutFieldFilter);
        }

        [Fact]
        public async void ObjectsWithMultipleDynamicFieldsUsingTheSameFieldNamesShouldRaiseError()
        {
            var exception = await Assert.ThrowsAsync<LiftiException>(
                async () => await CreateDynamicObjectTestIndexAsync(false));

            exception.Message.Should().Be(
                "A duplicate field \"Field1\" was encountered while indexing item A. Most likely multiple dynamic field providers have been configured " +
                "and the same field was produced by more than one of them. Consider using a field prefix when configuring the dynamic fields.");
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
            await this.PopulateIndexWithWikipediaData();

            var results = this.index.Search("data").ToList();
            results.Should().BeInDescendingOrder(r => r.Score);
            results.First().Score.Should().BeApproximately(2.434951738D, 0.0001D);
            results.Last().Score.Should().BeApproximately(1.2298017649D, 0.0001D);
        }

        [Fact]
        public async Task QueringIndex_ShouldHandleWildcards()
        {
            await this.PopulateIndexWithWikipediaData();

            var results = this.index.Search("*ta").ToList();
            results.Should().BeInDescendingOrder(r => r.Score);
        }

        [Fact]
        public async Task WhenLoadingLotsOfDataShouldNotError()
        {
            var wikipediaTests = await this.PopulateIndexWithWikipediaData();

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

            this.index.Root.ToString().Should().BeEquivalentTo(previousRoot.ToString());
        }

        [Fact]
        public async Task AddingItemsToIndex_ShouldUseProvidedTextExtractor()
        {
            var textExtractor = new FakeTextExtractor(
                new DocumentTextFragment(0, "MOCKED".AsMemory()));

            var index = new FullTextIndexBuilder<int>()
                .WithIntraNodeTextSupportedAfterIndexDepth(0)
                .WithTextExtractor(textExtractor)
                .Build();

            await index.AddAsync(1, "Hello");

            index.Root.IntraNodeText.ToString().Should().BeEquivalentTo("MOCKED");
        }

        [Fact]
        public async Task SearchingTheIndex_ShouldNotUseTextExtractor()
        {
            var index = new FullTextIndexBuilder<int>()
                .WithIntraNodeTextSupportedAfterIndexDepth(0)
                .WithTextExtractor<ReversingTextExtractor>()
                .Build();

            await index.AddAsync(1, "Hello");

            // The text will have been reversed by the text extractor, but searching won't have that applied
            index.Search("Hello").Should().HaveCount(0);
            index.Search("olleh").Should().HaveCount(1);
        }

        [Fact]
        public async Task ScoreBoostingAField_ShouldResultInBoostedSearchResults()
        {
            var index = new FullTextIndexBuilder<string>()
                .WithObjectTokenization<TestObject3>(
                    o => o.WithKey(i => i.Id)
                        .WithField("Text", i => i.Text, scoreBoost: 10D)
                        .WithField("MultiText", i => i.MultiText))
                .Build();


            await index.AddAsync(new TestObject3("A", "Test Two", "Test One"));
            await index.AddAsync(new TestObject3("B", "Test One", "Test Two"));

            // "B" should be returned first, and have 10x the score of "A" because of the 10x boost on the Text field
            var results = index.Search("One").ToList();
            results.Select(x => x.Key).Should().BeEquivalentTo(new[] { "B", "A" }, o => o.WithStrictOrdering());

            results[0].Score.Should().Be(results[1].Score * 10);
        }

        [Fact]
        public async Task ScoreBoostingADynamicField_ShouldResultInBoostedSearchResults()
        {
            var index = await CreateDynamicObjectTestIndexAsync(true, dynamicField1ScoreBoost: 10D);

            await index.AddAsync(
                new DynamicObject(
                    "C",
                    "Text One",
                    new Dictionary<string, string> { { "Field1", "Angry" } },
                    new ExtraField("Field1", "Happy")));

            await index.AddAsync(
                new DynamicObject(
                    "D",
                    "Text One",
                    new Dictionary<string, string> { { "Field1", "Happy" } },
                    new ExtraField("Field1", "Angry")));

            // "D" should be returned first, and have 10x the score of "C" because of the 10x boost on the dynamic field
            var results = index.Search("Happy").ToList();
            results.Select(x => x.Key).Should().BeEquivalentTo(new[] { "D", "C" }, o => o.WithStrictOrdering());
        }

        private static async Task<FullTextIndex<string>> CreateDynamicObjectTestIndexAsync(
            bool usePrefixes = false,
            double dynamicField1ScoreBoost = 1D,
            double dynamicField2ScoreBoost = 1D)
        {
            var index = new FullTextIndexBuilder<string>()
                .WithObjectTokenization<DynamicObject>(
                    o => o.WithKey(i => i.Id)
                        .WithField("Details", i => i.Details)
                        .WithDynamicFields("Dyn", i => i.DynamicFields, usePrefixes ? "Dyn1" : null, scoreBoost: dynamicField1ScoreBoost)
                        .WithDynamicFields("Extra", i => i.ExtraFields, x => x.Name, x => x.Value, usePrefixes ? "Dyn2" : null, scoreBoost: dynamicField2ScoreBoost))
                .Build();

            await index.AddAsync(
                new DynamicObject(
                    "A",
                    "Text One",
                    new Dictionary<string, string> { { "Field1", "Text One" }, { "Field2", "Text Two" } },
                    new ExtraField("Field1", "Alternative Text One"),
                    new ExtraField("Field2", "Alternative Text Two")));

            await index.AddAsync(
                new DynamicObject(
                    "B",
                    "Text Two",
                    new Dictionary<string, string> { { "Field1", "Not One" }, { "Field2", "Not Two" }, { "Field3", "Not Three" } }));

            return index;
        }

        private async Task WithIndexedSingleStringPropertyObjectsAsync()
        {
            await this.index.AddAsync(new TestObject("A", "Text One", "Text Two", "Text Three Drumming"));
            await this.index.AddAsync(new TestObject("B", "Not One", "Not Two", "Not Three Eat Eating"));
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
            await this.index.AddAsync("D", new[] { "One last test just for testing sake", "with hyphenated text: third-eye" });
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

        public record TestObject2(string Id, params string[] Text);

        public record TestObject3(string Id, string Text, params string[] MultiText);

        public class DynamicObject(string id, string details, Dictionary<string, string> dynamicFields, params FullTextIndexTests.ExtraField[] extraFields)
        {
            public string Id { get; } = id;
            public string Details { get; } = details;
            public Dictionary<string, string> DynamicFields { get; } = dynamicFields;
            public ExtraField[]? ExtraFields { get; } = extraFields.Length == 0 ? null : extraFields;
        }

        public record ExtraField(string Name, string Value);
    }
}

using FluentAssertions;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Lifti.Tests
{
    public class SearchResultsTests : IAsyncLifetime
    {
        private FullTextIndex<int> index = null!;
        private Dictionary<int, string> defaultFieldTestData = null!;
        private Dictionary<int, TestData> testData = null!;
        private Dictionary<int, TestDataWithArray> testDataWithArray = null!;

        public Task DisposeAsync()
        {
            return Task.CompletedTask;
        }

        public async Task InitializeAsync()
        {
            this.index = new FullTextIndexBuilder<int>()
               .WithObjectTokenization<TestData>(
                   o => o.WithKey(x => x.Id).WithField("SimpleText", x => x.Text, op => op.CaseInsensitive()))
               .WithObjectTokenization<TestDataWithArray>(
                   o => o.WithKey(x => x.Id).WithField("ArrayText", x => x.Text, op => op.CaseInsensitive()))
               .Build();

            this.testData = new[]
            {
                new TestData(1, "The quick brown fox"),
                    new TestData(2, "Also the big brown bear"),
                    new TestData(3, "But not the other quick fox or the brown owl")
            }.ToDictionary(x => x.Id);

            this.defaultFieldTestData = this.testData.ToDictionary(x => x.Key + 100, x => x.Value.Text);

            this.testDataWithArray = new[]
            {
                new TestDataWithArray(4, "The quick ", "brown", " fox"),
                new TestDataWithArray(5, "Also the ", "big brown ", "bear"),
                new TestDataWithArray(6, "But not the other quick ", "fox or the brown owl")
            }.ToDictionary(x => x.Id);

            index.BeginBatchChange();

            await this.index.AddRangeAsync(this.testData.Values);

            await this.index.AddRangeAsync(this.testDataWithArray.Values);

            foreach (var (key, value) in this.defaultFieldTestData)
            {
                await this.index.AddAsync(key, value);
            }

            await index.CommitBatchChangeAsync();
        }

        [Fact]
        public async Task CreateMatchPhrasesAsync_WithSimpleText_ShouldReturnCorrectPhrases()
        {
            var sut = this.index.Search("quick | brown | fox");

            var phrases = await sut.CreateMatchPhrasesAsync(x => testData[x]);

            phrases.Should().BeEquivalentTo(
                new[]
                {
                    new MatchedPhrases<int, TestData>(this.testData[1], 1, new[] { "quick brown fox" }),
                    new MatchedPhrases<int, TestData>(this.testData[2], 2, new[] { "brown" }),
                    new MatchedPhrases<int, TestData>(this.testData[3], 3, new[] { "quick fox", "brown" })
                });
        }

        [Fact]
        public async Task CreateMatchPhrasesAsync_WithTextArray_ShouldReturnCorrectPhrases()
        {
            var sut = this.index.Search("quick | brown | fox");

            var phrases = await sut.CreateMatchPhrasesAsync(x => testDataWithArray[x]);

            phrases.Should().BeEquivalentTo(
                new[]
                {
                    new MatchedPhrases<int, TestDataWithArray>(this.testDataWithArray[4], 4, new[] { "quick brown fox" }),
                    new MatchedPhrases<int, TestDataWithArray>(this.testDataWithArray[5], 5, new[] { "brown" }),
                    new MatchedPhrases<int, TestDataWithArray>(this.testDataWithArray[6], 6, new[] { "quick fox", "brown" })
                });
        }

        [Fact]
        public async Task CreateMatchPhrasesAsync_WithSimpleTextLoadedAsync_ShouldReturnCorrectPhrases()
        {
            var sut = this.index.Search("quick | brown | fox");

            var phrases = await sut.CreateMatchPhrasesAsync(async x => await Task.Run(() => testData[x]));

            phrases.Should().BeEquivalentTo(
                new[]
                {
                    new MatchedPhrases<int, TestData>(this.testData[1], 1, new[] { "quick brown fox" }),
                    new MatchedPhrases<int, TestData>(this.testData[2], 2, new[] { "brown" }),
                    new MatchedPhrases<int, TestData>(this.testData[3], 3, new[] { "quick fox", "brown" })
                });
        }

        [Fact]
        public async Task CreateMatchPhrasesAsync_WithTextArrayLoadedAsync_ShouldReturnCorrectPhrases()
        {
            var sut = this.index.Search("quick | brown | fox");

            var phrases = await sut.CreateMatchPhrasesAsync(async x => await Task.Run(() => testDataWithArray[x]));

            phrases.Should().BeEquivalentTo(
                new[]
                {
                    new MatchedPhrases<int, TestDataWithArray>(this.testDataWithArray[4], 4, new[] { "quick brown fox" }),
                    new MatchedPhrases<int, TestDataWithArray>(this.testDataWithArray[5], 5, new[] { "brown" }),
                    new MatchedPhrases<int, TestDataWithArray>(this.testDataWithArray[6], 6, new[] { "quick fox", "brown" })
                });
        }

        [Fact]
        public async Task CreateMatchPhrasesAsync_ForDefaultFieldText_ShouldReturnCorrectPhrases()
        {
            var sut = this.index.Search("quick | brown | fox");

            var phrases = await sut.CreateMatchPhrasesAsync(x => this.defaultFieldTestData[x]);

            phrases.Should().BeEquivalentTo(
                new[]
                {
                    new MatchedPhrases<int>(101, new[] { "quick brown fox" }),
                    new MatchedPhrases<int>(102, new[] { "brown" }),
                    new MatchedPhrases<int>(103, new[] { "quick fox", "brown" })
                });
        }

        [Fact]
        public async Task CreateMatchPhrasesAsync_ForDefaultFieldTextLoadedAsync_ShouldReturnCorrectPhrases()
        {
            var sut = this.index.Search("quick | brown | fox");

            var phrases = await sut.CreateMatchPhrasesAsync(async x => await Task.Run(() => defaultFieldTestData[x]));

            phrases.Should().BeEquivalentTo(
                new[]
                {
                    new MatchedPhrases<int>(101, new[] { "quick brown fox" }),
                    new MatchedPhrases<int>(102, new[] { "brown" }),
                    new MatchedPhrases<int>(103, new[] { "quick fox", "brown" })
                });
        }

        private record TestData(int Id, string Text);
        private record TestDataWithArray(int Id, params string[] Text);
    }
}

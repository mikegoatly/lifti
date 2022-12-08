using FluentAssertions;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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
        private ISearchResults<int> sut = null!;

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

            this.index.BeginBatchChange();

            await this.index.AddRangeAsync(this.testData.Values);

            await this.index.AddRangeAsync(this.testDataWithArray.Values);

            foreach (var (key, value) in this.defaultFieldTestData)
            {
                await this.index.AddAsync(key, value);
            }

            await this.index.CommitBatchChangeAsync();

            this.sut = this.index.Search("quick | brown | fox");
        }

        [Fact]
        public async Task CreateMatchPhrasesAsync_WithSimpleText_ShouldReturnCorrectPhrases()
        {
            var phrases = await this.sut.CreateMatchPhrasesAsync(x => this.testData[x]);

            this.VerifyObjectResults(this.testData, phrases);
        }

        [Fact]
        public async Task CreateMatchPhrasesAsync_WithTextArray_ShouldReturnCorrectPhrases()
        {
            var phrases = await this.sut.CreateMatchPhrasesAsync(x => this.testDataWithArray[x]);

            this.VerifyObjectResults(this.testDataWithArray, phrases);
        }

        [Fact]
        public async Task CreateMatchPhrasesAsync_WithSimpleTextLoadedAsync_ShouldReturnCorrectPhrases()
        {
            var phrases = await this.sut.CreateMatchPhrasesAsync(async x => await Task.Run(() => this.testData[x]));

            this.VerifyObjectResults(this.testData, phrases);
        }

        [Fact]
        public async Task CreateMatchPhrasesAsync_WithSimpleTextLoadedAsync_Uncancelled_ShouldReturnCorrectPhrases()
        {
            var phrases = await this.sut.CreateMatchPhrasesAsync(async (x, ct) => await Task.Run(() => this.testData[x], ct));

            this.VerifyObjectResults(this.testData, phrases);
        }

        [Fact]
        public async Task CreateMatchPhrasesAsync_WithSimpleTextLoadedAsync_Cancelled_ShouldThrowTaskCancelledException()
        {
            var cts = new CancellationTokenSource();
            cts.Cancel();

            await Assert.ThrowsAsync<TaskCanceledException>(
                async () => await this.sut.CreateMatchPhrasesAsync(async (x, ct) => await Task.Run(() => this.testData[x], ct), cts.Token));
        }

        [Fact]
        public async Task CreateMatchPhrasesAsync_WithTextArrayLoadedAsync_ShouldReturnCorrectPhrases()
        {
            var phrases = await this.sut.CreateMatchPhrasesAsync(async x => await Task.Run(() => this.testDataWithArray[x]));

            this.VerifyObjectResults(this.testDataWithArray, phrases);
        }

        [Fact]
        public async Task CreateMatchPhrasesAsync_WithTextArrayLoadedAsync_Uncancelled_ShouldReturnCorrectPhrases()
        {
            var phrases = await this.sut.CreateMatchPhrasesAsync(async (x, ct) => await Task.Run(() => this.testDataWithArray[x], ct));

            this.VerifyObjectResults(this.testDataWithArray, phrases);
        }

        [Fact]
        public async Task CreateMatchPhrasesAsync_WithTextArrayLoadedAsync_Cancelled_ShouldThrowTaskCancelledException()
        {
            var cts = new CancellationTokenSource();
            cts.Cancel();

            await Assert.ThrowsAsync<TaskCanceledException>(
                async () => await this.sut.CreateMatchPhrasesAsync(async (x, ct) => await Task.Run(() => this.testDataWithArray[x], ct), cts.Token));
        }

        [Fact]
        public void CreateMatchPhrases_ForDefaultFieldText_ShouldReturnCorrectPhrases()
        {
            var phrases = this.sut.CreateMatchPhrases(x => this.defaultFieldTestData[x]);

            VerifyDefaultFieldPhrases(phrases);
        }

        [Fact]
        public async Task CreateMatchPhrasesAsync_ForDefaultFieldTextLoadedAsync_ShouldReturnCorrectPhrases()
        {
            var phrases = await this.sut.CreateMatchPhrasesAsync(async x => await Task.Run(() => this.defaultFieldTestData[x]));

            VerifyDefaultFieldPhrases(phrases);
        }

        [Fact]
        public async Task CreateMatchPhrasesAsync_ForDefaultFieldTextLoadedAsync_Uncancelled_ShouldReturnCorrectPhrases()
        {
            var phrases = await this.sut.CreateMatchPhrasesAsync(async (x, ct) => await Task.Run(() => this.defaultFieldTestData[x], ct));

            VerifyDefaultFieldPhrases(phrases);
        }

        [Fact]
        public async Task CreateMatchPhrasesAsync_ForDefaultFieldTextLoadedAsync_Cancelled_ShouldThrowTaskCancelledException()
        {
            var cts = new CancellationTokenSource();
            cts.Cancel();

            await Assert.ThrowsAsync<TaskCanceledException>(
                async () => await this.sut.CreateMatchPhrasesAsync(async (x, ct) => await Task.Run(() => this.defaultFieldTestData[x], ct), cts.Token));
        }

        private static void VerifyDefaultFieldPhrases(IEnumerable<MatchedPhrases<int>> phrases)
        {
            phrases.Should().BeEquivalentTo(
                new[]
                            {
                    new MatchedPhrases<int>(101, new[] { "quick brown fox" }),
                    new MatchedPhrases<int>(102, new[] { "brown" }),
                    new MatchedPhrases<int>(103, new[] { "quick fox", "brown" })
                 });
        }

        private void VerifyObjectResults<TItem>(Dictionary<int, TItem> sourceItems, IEnumerable<MatchedPhrases<int, TItem>> phrases)
        {
            var source = sourceItems.ToList();

            phrases.Should().BeEquivalentTo(
                new[]
                {
                    new MatchedPhrases<int, TItem>(source[0].Value, source[0].Key, new[] { "quick brown fox" }),
                    new MatchedPhrases<int, TItem>(source[1].Value, source[1].Key, new[] { "brown" }),
                    new MatchedPhrases<int, TItem>(source[2].Value, source[2].Key, new[] { "quick fox", "brown" })
                });
        }

        private record TestData(int Id, string Text);
        private record TestDataWithArray(int Id, params string[] Text);
    }
}

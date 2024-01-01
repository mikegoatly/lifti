using FluentAssertions;
using Lifti.Querying;
using Lifti.Querying.QueryParts;
using Lifti.Tests.Fakes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Lifti.Tests.Querying.QueryParts
{
    public class FuzzyWordQueryPartTestsFixture : IAsyncLifetime
    {
        public string[] IndexedText { get; } = {
            "Some sample comics text to match on",
            "Samples sounds like a solid plan to me",
            "Odius ogres obey Mobius"
        };

        public FullTextIndex<int> Index { get; private set; } = null!;

        public Task DisposeAsync()
        {
            return Task.CompletedTask;
        }

        public async Task InitializeAsync()
        {
            this.Index = new FullTextIndexBuilder<int>()
                .Build();

            this.Index.BeginBatchChange();

            for (var i = 0; i < this.IndexedText.Length; i++)
            {
                await this.Index.AddAsync(i, this.IndexedText[i]);
            }

            await this.Index.CommitBatchChangeAsync();
        }
    }

    public class FuzzyWordQueryPartTests : IClassFixture<FuzzyWordQueryPartTestsFixture>
    {

        private readonly ITestOutputHelper outputHelper;
        private readonly FuzzyWordQueryPartTestsFixture fixture;

        public FuzzyWordQueryPartTests(ITestOutputHelper outputHelper, FuzzyWordQueryPartTestsFixture fixture)
        {
            this.outputHelper = outputHelper;
            this.fixture = fixture;
        }

        [Fact]
        public void ShouldReturnExactMatch()
        {
            this.RunTest("SAMPLES", 2, 1, "samples");
        }

        [Fact]
        public void MaxDistanceOfOne_ShouldHandleSubstitutions()
        {
            this.RunTest("SONE", 1, 1, "some");
        }

        [Fact]
        public void MaxDistanceOfOne_ShouldHandleDeletions()
        {
            this.RunTest("SOE", 1, 1, "some");
        }

        [Fact]
        public void MaxDistanceOfOne_ShouldHandleInsertions()
        {
            this.RunTest("SOMME", 1, 1, "some");
        }

        [Fact]
        public void MaxDistanceOfOne_ShouldTreatTranspositionsAsSingleEdit()
        {
            this.RunTest("SMOE", 1, 1, "some");
        }

        [Fact]
        public void MaxDistanceOfThree_ShouldReturnAllPotentialVariations()
        {
            this.RunTest("OBAN", 3, 3, "obey", "plan", "on");
        }

        [Fact]
        public void ShouldNotAllowVariationsConsistingEntirelyOfEdits()
        {
            this.RunTest("OF", 2, 1, "on");
        }

        [Fact]
        public void WhenMatchEndsOnExactMatch_PotentialDeletionsShouldStillBeReturned()
        {
            this.RunTest("SAMPE", 2, 1, "some", "sample", "samples");
        }

        [Fact]
        public async Task WithFieldFilteredInContext_ShouldOnlyMatchOnRequestedField()
        {
            var index = new FullTextIndexBuilder<int>()
                .WithObjectTokenization<TestObject>(
                o => o.WithKey(x => x.Id)
                    .WithField("title", x => x.Title)
                    .WithField("content", x => x.Content))
                .Build();

            await index.AddRangeAsync(new[]
            {
                new TestObject(1, "Item number 1", "Item number one content"),
                new TestObject(2, "Second", "Item number two content"),
                new TestObject(3, "Item number 3", "Item number three content")
            });

            var query = new Query(
                FieldFilterQueryOperator.CreateForField(
                    index.FieldLookup, 
                    "title", 
                    new FuzzyMatchQueryPart("NUMBE", 1, 1)));

            var results = index.Search(query).ToList();

            results.Select(x => x.Key).Should().BeEquivalentTo(new[] { 1, 3 });
        }

        [Fact]
        public void ToString_WithDefaultParameters_ShouldReturnSimpleExpression()
        {
            new FuzzyMatchQueryPart("Test").ToString().Should().Be("?Test");
        }

        [Theory]
        [InlineData(null, 4, "?,4?Test")]
        [InlineData(9, null, "?9,?Test")]
        [InlineData(9, 5, "?9,5?Test")]
        public void ToString_WithParameters_ShouldReturnCorrectlyFormattedExpression(int? maxEditDistance, int? maxSequentialEdits, string expectedOutput)
        {
            new FuzzyMatchQueryPart("Test", (ushort?)maxEditDistance ?? FuzzyMatchQueryPart.DefaultMaxEditDistance, (ushort?)maxSequentialEdits ?? FuzzyMatchQueryPart.DefaultMaxSequentialEdits)
                .ToString().Should().Be(expectedOutput);
        }

        [Fact]
        public void ToString_WithScoreBoost_ShouldReturnCorrectlyFormattedExpression()
        {
            new FuzzyMatchQueryPart("Test", 1, 3, 5.123).ToString().Should().Be("?1,3?Test^5.123");
        }

        [Fact]
        public void WhenFuzzyMatchingWord_ScoreShouldBeLessThanExactMatch()
        {
            var exactMatchScore = this.GetScore("SAMPLE", 1, 1);
            var singleEditMatchScore = this.GetScore("SXMPLE", 1, 1);
            var twoEditsMatchScore = this.GetScore("SXMPXE", 2, 1);
            var threeEditsMatchScore = this.GetScore("SXMXLX", 3, 1);

            var expectedScoreOrders = new[] { exactMatchScore, singleEditMatchScore, twoEditsMatchScore, threeEditsMatchScore };

            expectedScoreOrders.Should().BeInDescendingOrder();
        }

        [Fact]
        public void WhenScoreBoosting_ShouldApplyBoostToScore()
        {
            var baseScore = this.GetScore("SAMPLE", 1, 1);
            var boostedScore = this.GetScore("SAMPLE", 1, 1, 2D);

            boostedScore.Should().Be(baseScore * 2D);
        }

        private double GetScore(string search, ushort maxDistance, ushort maxSequentialEdits, double? scoreBoost = null)
        {
            var part = new FuzzyMatchQueryPart(search, maxDistance, maxSequentialEdits, scoreBoost);
            var results = this.fixture.Index.Search(new Query(part)).ToList();
            return results.Where(r => r.FieldMatches.Any(m => m.Locations.Any(l => l.TokenIndex == 1)) && r.Key == 0)
                .Select(s => s.Score)
                .Single();
        }

        private void RunTest(string word, ushort maxEditDistance, ushort maxSequentialEdits, params string[] expectedWords)
        {
            var expectedWordLookup = expectedWords.ToHashSet(StringComparer.OrdinalIgnoreCase);
            var expectedMatchRegex = new Regex(@"(^|\s)*((?<word>[^\s]*)($|\s))+");
            var expectedResultCaptures = this.fixture.IndexedText.Select(
                (text, id) =>
                    (
                    id,
                    expectedMatchRegex.Match(text).Groups["word"]
                        .Captures
                        .OfType<Capture>()
                        .Select((x, index) => (index, startLocation: x.Index, x.Value))
                        .Where(c => expectedWordLookup.Contains(c.Value))
                        .ToList()
                    ))
                .Where(r => r.Item2.Count > 0);

            // Double-check that each of the expected words has been translated to its matching positions in the test articles
            expectedResultCaptures.SelectMany(s => s.Item2.Select(i => i.Value)).ToHashSet(StringComparer.OrdinalIgnoreCase)
                .Should().HaveCount(expectedWords.Length, because: "Each of the expected words should be found at least once in the source articles");

            var expectedResults = expectedResultCaptures.Select(
                r => Tuple.Create(
                    r.id, 
                    r.Item2.Select(
                        x => new TokenLocation(x.index, x.startLocation, (ushort)x.Value.Length)).ToList()
                    ))
                .ToList();

            var part = new FuzzyMatchQueryPart(word, maxEditDistance, maxSequentialEdits);

            var results = this.fixture.Index.Search(new Query(part)).ToList();

            this.outputHelper.WriteLine("Expected matches:");
            this.WriteMatches(expectedResults);

            this.outputHelper.WriteLine("Actual matches:");
            this.WriteMatches(results.Select(r => Tuple.Create(r.Key, r.FieldMatches.SelectMany(m => m.Locations).ToList())));

            results.Should().HaveCount(expectedResults.Count());

            foreach (var expectedResult in expectedResults)
            {
                results.Single(r => r.Key == expectedResult.Item1).FieldMatches.Should().SatisfyRespectively(
                    x => x.Locations.Should().BeEquivalentTo(expectedResult.Item2));
            }
        }

        private void WriteMatches(IEnumerable<Tuple<int, List<TokenLocation>>> results)
        {
            foreach (var result in results)
            {
                this.outputHelper.WriteLine("");
                this.outputHelper.WriteLine("Item " + result.Item1);

                foreach (var match in result.Item2)
                {
                    this.outputHelper.WriteLine($"index {match.TokenIndex} start {match.Start}: {this.fixture.IndexedText[result.Item1].Substring(match.Start, match.Length)}");
                }

            }
        }

        private record TestObject(int Id, string Title, string Content);
    }
}

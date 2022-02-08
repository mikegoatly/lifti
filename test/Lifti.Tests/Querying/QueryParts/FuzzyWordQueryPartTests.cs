using FluentAssertions;
using Lifti.Querying;
using Lifti.Querying.QueryParts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

        public FullTextIndex<int> Index { get; private set; }

        public Task DisposeAsync()
        {
            return Task.CompletedTask;
        }

        public async Task InitializeAsync()
        {
            this.Index = new FullTextIndexBuilder<int>()
                .Build();

            this.Index.BeginBatchChange();

            for (var i = 0; i < IndexedText.Length; i++)
            {
                await this.Index.AddAsync(i, IndexedText[i]);
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
            RunTest("SAMPLES", 2, 1, "samples");
        }

        [Fact]
        public void MaxDistanceOfOne_ShouldHandleSubstitutions()
        {
            RunTest("SONE", 1, 1, "some");
        }

        [Fact]
        public void MaxDistanceOfOne_ShouldHandleDeletions()
        {
            RunTest("SOE", 1, 1, "some");
        }

        [Fact]
        public void MaxDistanceOfOne_ShouldHandleInsertions()
        {
            RunTest("SOMME", 1, 1, "some");
        }

        [Fact]
        public void MaxDistanceOfOne_ShouldTreatTranspositionsAsSingleEdit()
        {
            RunTest("SMOE", 1, 1, "some");
        }

        [Fact]
        public void MaxDistanceOfThree_ShouldReturnAllPotentialVariations()
        {
            RunTest("OBAN", 3, 3, "obey", "plan", "on");
        }

        [Fact]
        public void ShouldNotAllowVariationsConsistingEntirelyOfEdits()
        {
            RunTest("OF", 2, 1, "on");
        }

        [Fact]
        public void WhenMatchEndsOnExactMatch_PotentialDeletionsShouldStillBeReturned()
        {
            RunTest("SAMPE", 2, 1, "some", "sample", "samples");
        }

        [Fact]
        public void WhenFuzzyMatchingWord_ScoreShouldBeLessThanExactMatch()
        {
            var exactMatchScore = GetScore("SAMPLE", 1, 1);
            var singleEditMatchScore = GetScore("SXMPLE", 1, 1);
            var twoEditsMatchScore = GetScore("SXMPXE", 2, 1);
            var threeEditsMatchScore = GetScore("SXMXLX", 3, 1);

            var expectedScoreOrders = new[] { exactMatchScore, singleEditMatchScore, twoEditsMatchScore, threeEditsMatchScore };

            expectedScoreOrders.Should().BeInDescendingOrder();
        }

        private double GetScore(string search, ushort maxDistance, ushort maxSequentialEdits)
        {
            var part = new FuzzyMatchQueryPart(search, maxDistance, maxSequentialEdits);
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
                        .Select((x, index) => (index, startLocation: x.Index, x.Value))
                        .Where(c => expectedWordLookup.Contains(c.Value))
                        .ToList()
                    ))
                .Where(r => r.Item2.Count > 0);

            // Double-check that each of the expected words has been translated to its matching positions in the test articles
            expectedResultCaptures.SelectMany(s => s.Item2.Select(i => i.Value)).ToHashSet(StringComparer.OrdinalIgnoreCase)
                .Should().HaveCount(expectedWords.Length, because: "Each of the expected words should be found at least once in the source articles");

            var expectedResults = expectedResultCaptures.Select(
                r => (r.id, r.Item2.Select(
                    x => new TokenLocation(x.index, x.startLocation, (ushort)x.Value.Length)).ToList()))
                .ToList();

            var part = new FuzzyMatchQueryPart(word, maxEditDistance, maxSequentialEdits);

            var results = this.fixture.Index.Search(new Query(part)).ToList();

            outputHelper.WriteLine("Expected matches:");
            WriteMatches(expectedResults);

            outputHelper.WriteLine("Actual matches:");
            WriteMatches(results.Select(r => (r.Key, r.FieldMatches.SelectMany(m => m.Locations).ToList())));

            results.Should().HaveCount(expectedResults.Count);

            foreach (var (expectedId, expectedLocations) in expectedResults)
            {
                results.Single(r => r.Key == expectedId).FieldMatches.Should().SatisfyRespectively(
                    x => x.Locations.Should().BeEquivalentTo(expectedLocations));
            }
        }

        private void WriteMatches(IEnumerable<(int id, List<TokenLocation>)> results)
        {
            foreach (var result in results)
            {
                outputHelper.WriteLine("");
                outputHelper.WriteLine("Item " + result.id);

                foreach (var match in result.Item2)
                {
                    outputHelper.WriteLine($"index {match.TokenIndex} start {match.Start}: {this.fixture.IndexedText[result.id].Substring(match.Start, match.Length)}");
                }

            }
        }
    }
}

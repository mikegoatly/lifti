using FluentAssertions;
using Lifti.Querying;
using Lifti.Querying.QueryParts;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Lifti.Tests.Querying.QueryParts
{
    public class WildcardQueryPartTests : IAsyncLifetime
    {
        private FullTextIndex<int> index = null!;

        public Task DisposeAsync()
        {
            return Task.CompletedTask;
        }

        public async Task InitializeAsync()
        {
            this.index = new FullTextIndexBuilder<int>()
                .Build();

            await this.index.AddAsync(1, "Apparently this also applies");
            await this.index.AddAsync(2, "Angry alternatives to apples, thus");
        }

        [Fact]
        public void Evaluating_WithSingleTextFragment_ShouldReturnOnlyExactMatches()
        {
            var part = new WildcardQueryPart([WildcardQueryFragment.CreateText("ALSO")]);
            var results = this.index.Search(new Query(part)).ToList();

            results.Should().HaveCount(1);

            results[0].FieldMatches.Should().SatisfyRespectively(
                x => x.Locations.Should().BeEquivalentTo(new[]
                {
                    new TokenLocation(2, 16, 4)
                }));
        }

        [Fact]
        public void Evaluating_WithSScoreBoost_ShouldApplyBoostToResultingScore()
        {
            var part = new WildcardQueryPart([WildcardQueryFragment.CreateText("ALSO")]);
            var unboostedScore = this.index.Search(new Query(part)).ToList()[0].Score;
            part = new WildcardQueryPart(new[] { WildcardQueryFragment.CreateText("ALSO") }, 2D);
            var boostedScore = this.index.Search(new Query(part)).ToList()[0].Score;

            boostedScore.Should().Be(unboostedScore * 2D);
        }

        [Fact]
        public void Evaluating_WithSingleCharacterReplacement_ShouldReturnCorrectResults()
        {
            var part = new WildcardQueryPart(
            [
                WildcardQueryFragment.CreateText("TH"),
                WildcardQueryFragment.SingleCharacter,
                WildcardQueryFragment.CreateText("S")
            ]);

            var results = this.index.Search(new Query(part)).ToList();

            results.Should().HaveCount(2);

            results[0].FieldMatches.Should().SatisfyRespectively(
                x => x.Locations.Should().BeEquivalentTo(new[]
                {
                    new TokenLocation(1, 11, 4)
                }));

            results[1].FieldMatches.Should().SatisfyRespectively(
                x => x.Locations.Should().BeEquivalentTo(new[]
                {
                    new TokenLocation(4, 30, 4)
                }));
        }

        [Fact]
        public void Evaluating_WithTerminatingSingleCharacterReplacement_ShouldReturnCorrectResults()
        {
            var part = new WildcardQueryPart(
            [
                WildcardQueryFragment.CreateText("TH"),
                WildcardQueryFragment.SingleCharacter,
                WildcardQueryFragment.SingleCharacter
            ]);

            var results = this.index.Search(new Query(part)).ToList();

            results.Should().HaveCount(2);

            results[0].FieldMatches.Should().SatisfyRespectively(
                x => x.Locations.Should().BeEquivalentTo(new[]
                {
                    new TokenLocation(1, 11, 4)
                }));

            results[1].FieldMatches.Should().SatisfyRespectively(
                x => x.Locations.Should().BeEquivalentTo(new[]
                {
                    new TokenLocation(4, 30, 4)
                }));
        }

        [Fact]
        public void Evaluating_WithTerminatingMultiCharacterReplacement_ShouldReturnAllMatchesStartingWithText()
        {
            var part = new WildcardQueryPart(
            [
                WildcardQueryFragment.CreateText("A"),
                WildcardQueryFragment.MultiCharacter
            ]);

            var results = this.index.Search(new Query(part)).ToList();

            results.Should().HaveCount(2);

            results[0].FieldMatches.Should().SatisfyRespectively(
                x => x.Locations.Should().BeEquivalentTo(new[]
                {
                    new TokenLocation(0, 0, 10),
                    new TokenLocation(2, 16, 4),
                    new TokenLocation(3, 21, 7),
                }));

            results[1].FieldMatches.Should().SatisfyRespectively(
                x => x.Locations.Should().BeEquivalentTo(new[]
                {
                    new TokenLocation(0, 0, 5),
                    new TokenLocation(1, 6, 12),
                    new TokenLocation(3, 22, 6)
                }));
        }

        [Fact]
        public void Evaluating_WithLeadingMultiCharacterReplacement_ShouldReturnAllMatchesEndingWithText()
        {
            var part = new WildcardQueryPart(
            [
                WildcardQueryFragment.MultiCharacter,
                WildcardQueryFragment.CreateText("ES")
            ]);

            var results = this.index.Search(new Query(part)).ToList();

            results.Should().HaveCount(2);

            results.Single(r => r.Key == 1).FieldMatches.Should().SatisfyRespectively(
                x => x.Locations.Should().BeEquivalentTo(new[]
                {
                    new TokenLocation(3, 21, 7)
                }));

            results.Single(r => r.Key == 2).FieldMatches.Should().SatisfyRespectively(
                x => x.Locations.Should().BeEquivalentTo(new[]
                {
                    new TokenLocation(1, 6, 12),
                    new TokenLocation(3, 22, 6)
                }));
        }

        [Fact]
        public void Evaluating_WithSequenceOfSingleCharacterWildcards_ShouldOnlyMatchWordsWithMatchingCharacterCounts()
        {
            var part = new WildcardQueryPart(
            [
                WildcardQueryFragment.SingleCharacter,
                WildcardQueryFragment.SingleCharacter
            ]);

            var results = this.index.Search(new Query(part)).ToList();

            results.Should().HaveCount(1);

            results.Single(r => r.Key == 2).FieldMatches.Should().SatisfyRespectively(
                x => x.Locations.Should().BeEquivalentTo(new[]
                {
                    new TokenLocation(2, 19, 2)
                }));
        }

        [Fact]
        public void Evaluating_WithSequenceOfSingleCharacterWildcardsFollowedByMultiCharacterWildcard_ShouldMatchWordsWithAtLeastCharacterCount()
        {
            var part = new WildcardQueryPart(
            [
                WildcardQueryFragment.SingleCharacter,
                WildcardQueryFragment.SingleCharacter,
                WildcardQueryFragment.SingleCharacter,
                WildcardQueryFragment.SingleCharacter,
                WildcardQueryFragment.SingleCharacter,
                WildcardQueryFragment.SingleCharacter,
                WildcardQueryFragment.MultiCharacter
            ]);

            var results = this.index.Search(new Query(part)).ToList();

            results.Should().HaveCount(2);

            results.Single(r => r.Key == 1).FieldMatches.Should().SatisfyRespectively(
                x => x.Locations.Should().BeEquivalentTo(new[]
                {
                    new TokenLocation(0, 0, 10),
                    new TokenLocation(3, 21, 7)
                }));

            results.Single(r => r.Key == 2).FieldMatches.Should().SatisfyRespectively(
                x => x.Locations.Should().BeEquivalentTo(new[]
                {
                    new TokenLocation(1, 6, 12),
                    new TokenLocation(3, 22, 6)
                }));
        }

        [Fact]
        public void Evaluating_WithConsecutiveSingleCharacterReplacement_ShouldReturnCorrectResults()
        {
            var part = new WildcardQueryPart(
            [
                WildcardQueryFragment.CreateText("T"),
                WildcardQueryFragment.SingleCharacter,
                WildcardQueryFragment.SingleCharacter,
                WildcardQueryFragment.CreateText("S")
            ]);

            var results = this.index.Search(new Query(part)).ToList();

            results.Should().HaveCount(2);

            results[0].FieldMatches.Should().SatisfyRespectively(
                x => x.Locations.Should().BeEquivalentTo(new[]
                {
                    new TokenLocation(1, 11, 4)
                }));

            results[1].FieldMatches.Should().SatisfyRespectively(
                x => x.Locations.Should().BeEquivalentTo(new[]
                {
                    new TokenLocation(4, 30, 4)
                }));
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
                    new WildcardQueryPart(WildcardQueryFragment.CreateText("NUMBE"), WildcardQueryFragment.MultiCharacter)));

            var results = index.Search(query).ToList();

            results.Select(x => x.Key).Should().BeEquivalentTo(new[] { 1, 3 });
        }

        private record TestObject(int Id, string Title, string Content);
    }
}

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
        private FullTextIndex<int> index;

        public WildcardQueryPartTests()
        {
        }

        [Fact]
        public void Evaluating_WithSingleTextFragment_ShouldReturnOnlyExactMatches()
        {
            var part = new WildcardQueryPart(new[] { WildcardQueryFragment.CreateText("ALSO") });
            var results = this.index.Search(new Query(part)).ToList();

            results.Should().HaveCount(1);

            results[0].FieldMatches.Should().SatisfyRespectively(
                x => x.Locations.Should().BeEquivalentTo(new[]
                {
                    new TokenLocation(2, 16, 4)
                }));
        }

        [Fact]
        public void Evaluating_WithSingleCharacterReplacement_ShouldReturnCorrectResults()
        {
            var part = new WildcardQueryPart(new[]
            {
                WildcardQueryFragment.CreateText("TH"),
                WildcardQueryFragment.SingleCharacter,
                WildcardQueryFragment.CreateText("S")
            });

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
            var part = new WildcardQueryPart(new[]
            {
                WildcardQueryFragment.CreateText("TH"),
                WildcardQueryFragment.SingleCharacter,
                WildcardQueryFragment.SingleCharacter
            });

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
            var part = new WildcardQueryPart(new[]
            {
                WildcardQueryFragment.CreateText("A"),
                WildcardQueryFragment.MultiCharacter
            });

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
            var part = new WildcardQueryPart(new[]
            {
                WildcardQueryFragment.MultiCharacter,
                WildcardQueryFragment.CreateText("ES")
            });

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
            var part = new WildcardQueryPart(new[]
            {
                WildcardQueryFragment.SingleCharacter,
                WildcardQueryFragment.SingleCharacter
            });

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
            var part = new WildcardQueryPart(new[]
            {
                WildcardQueryFragment.SingleCharacter,
                WildcardQueryFragment.SingleCharacter,
                WildcardQueryFragment.SingleCharacter,
                WildcardQueryFragment.SingleCharacter,
                WildcardQueryFragment.SingleCharacter,
                WildcardQueryFragment.SingleCharacter,
                WildcardQueryFragment.MultiCharacter
            });

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
            var part = new WildcardQueryPart(new[]
            {
                WildcardQueryFragment.CreateText("T"),
                WildcardQueryFragment.SingleCharacter,
                WildcardQueryFragment.SingleCharacter,
                WildcardQueryFragment.CreateText("S")
            });

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
    }
}

using FluentAssertions;
using Lifti.Querying;
using Lifti.Querying.QueryParts;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Lifti.Tests.Querying.QueryParts
{
    public class PrecedingNearQueryOperatorTests : QueryTestBase
    {
        [Fact]
        public void ShouldOnlyReturnMatchesForAppropriateField()
        {
            var sut = new PrecedingNearQueryOperator(
                new FakeQueryPart(
                    ScoredToken(7, ScoredFieldMatch(1D, 1, 8, 20, 100), ScoredFieldMatch(5D, 2, 9, 14)),
                    ScoredToken(8, ScoredFieldMatch(2D, 1, 11, 101), ScoredFieldMatch(6D, 2, 8, 104))),
                new FakeQueryPart(
                    ScoredToken(7, ScoredFieldMatch(3D, 1, 6, 14, 102)),
                    ScoredToken(8, ScoredFieldMatch(4D, 1, 5, 106), ScoredFieldMatch(7D, 2, 3, 105))));

            var results = sut.Evaluate(() => new FakeIndexNavigator(), QueryContext.Empty);

            // Item 7 matches:
            // Field 1: (100, 102)
            // Field 2: None
            // Item 8 matches:
            // Field 1: (101, 106)
            // Field 2: (104, 105)
            results.Matches.Should().BeEquivalentTo(
                new[]
                {
                    ScoredToken(
                        7,
                        ScoredFieldMatch(4D, 1, CompositeTokenLocation(100, 102))),
                    ScoredToken(
                        8,
                        ScoredFieldMatch(6D, 1, CompositeTokenLocation(101, 106)),
                        ScoredFieldMatch(13D, 2, CompositeTokenLocation(104, 105)))
                });
        }

        [Fact]
        public async Task ShouldOnlyReturnResultsWhereFirstWordIsBeforeSecond()
        {
            var index = await CreateTestIndexAsync();

            var results = index.Search("critical ~> acclaim");

            results.Should().HaveCount(1);
            var result = results.Single();
            result.Key.Should().Be(4);

            var fieldMatch = result.FieldMatches.Single();
            fieldMatch.Locations.Should().BeEquivalentTo(
                new[]
                {
                    new TokenLocation(11, 67, 8),
                    new TokenLocation(12, 76, 7)
                });
        }

        [Fact]
        public void CalculateWeighting_ShouldReturnSmallestWeightingOfParts()
        {
            var op = new PrecedingNearQueryOperator(new FakeQueryPart(3D), new FakeQueryPart(2D));

            op.CalculateWeighting(() => new FakeIndexNavigator()).Should().Be(2D);
        }

        protected static async Task<IFullTextIndex<int>> CreateTestIndexAsync()
        {
            var index = new FullTextIndexBuilder<int>()
                .WithDefaultTokenization(o => o.WithStemming())
                .Build();

            await index.AddAsync(1, "One two three four five");
            await index.AddAsync(2, "Five four three two one");
            await index.AddAsync(3, "One Nine six");
            await index.AddAsync(4, "During a career spanning more than 20 years, Porcupine Tree earned critical acclaim from critics and fellow musicians, developed a cult following, and became an influence for new artists");

            return index;
        }
    }
}
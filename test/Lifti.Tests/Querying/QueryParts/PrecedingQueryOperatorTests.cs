using FluentAssertions;
using Lifti.Querying;
using Lifti.Querying.QueryParts;
using Xunit;

namespace Lifti.Tests.Querying.QueryParts
{
    public class PrecedingQueryOperatorTests : QueryTestBase
    {
        [Fact]
        public void ShouldOnlyReturnMatchesForAppropriateField()
        {
            var sut = new PrecedingQueryOperator(
                new FakeQueryPart(
                    ScoredToken(7, ScoredFieldMatch(1D, 1, 8, 20, 100), ScoredFieldMatch(5D, 2, 9, 14)),
                    ScoredToken(8, ScoredFieldMatch(2D, 1, 11, 101), ScoredFieldMatch(6D, 2, 8, 104))),
                new FakeQueryPart(
                    ScoredToken(7, ScoredFieldMatch(3D, 1, 6, 14, 102)),
                    ScoredToken(8, ScoredFieldMatch(4D, 1, 5, 106), ScoredFieldMatch(7D, 2, 3, 105))));

            var results = sut.Evaluate(() => new FakeIndexNavigator(), QueryContext.Empty);

            // Item 7 matches:
            // Field 1: 8, 14, 102, 20, 100
            // Field 2: None
            // Item 8 matches:
            // Field 1: 11, 106, 101
            // Field 2: 8, 105, 104
            results.Matches.Should().BeEquivalentTo(
                new[]
                {
                    ScoredToken(
                        7,
                        ScoredFieldMatch(4D, 1, TokenMatch(8), TokenMatch(14), TokenMatch(20), TokenMatch(100), TokenMatch(102))),
                    ScoredToken(
                        8,
                        ScoredFieldMatch(6D, 1, TokenMatch(11), TokenMatch(101), TokenMatch(106)),
                        ScoredFieldMatch(13D, 2, TokenMatch(8), TokenMatch(104), TokenMatch(105)))
                });
        }

        [Fact]
        public void CalculateWeighting_ShouldReturnSmallestWeightingOfParts()
        {
            var op = new PrecedingQueryOperator(new FakeQueryPart(3D), new FakeQueryPart(2D));

            op.CalculateWeighting(() => new FakeIndexNavigator()).Should().Be(2D);
        }
    }
}

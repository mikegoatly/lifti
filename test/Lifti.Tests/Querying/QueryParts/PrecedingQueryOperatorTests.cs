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
                ScoredToken(
                    7,
                    ScoredFieldMatch(4D, 1, WordMatch(8), WordMatch(14), WordMatch(20), WordMatch(100), WordMatch(102))),
                ScoredToken(
                    8,
                    ScoredFieldMatch(6D, 1, WordMatch(11), WordMatch(101), WordMatch(106)),
                    ScoredFieldMatch(13D, 2, WordMatch(8), WordMatch(104), WordMatch(105))));
        }
    }
}

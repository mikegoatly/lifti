using FluentAssertions;
using Lifti.Querying;
using Lifti.Querying.QueryParts;
using Xunit;

namespace Lifti.Tests.Querying.QueryParts
{
    public class FieldFilterQueryOperatorTests : QueryTestBase
    {
        [Fact]
        public void ShouldFilterAllItemResultsToRequiredField()
        {
            var navigator = FakeIndexNavigator.ReturningExactMatches(
                ScoredToken(2, ScoredFieldMatch(1D, 2, 1, 2), ScoredFieldMatch(2D, 4, 1)),
                ScoredToken(4, ScoredFieldMatch(3D, 3, 3), ScoredFieldMatch(4D, 4, 44, 99), ScoredFieldMatch(5D, 5, 2)));

            var sut = new FieldFilterQueryOperator("Test", 4, new ExactWordQueryPart("x"));

            var results = sut.Evaluate(() => navigator, QueryContext.Empty);

            results.Matches.Should().BeEquivalentTo(
                new[]
                {
                    ScoredToken(2, ScoredFieldMatch(2D, 4, 1)),
                    ScoredToken(4, ScoredFieldMatch(4D, 4, 44, 99))
                });
        }
    }
}

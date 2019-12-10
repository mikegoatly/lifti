using FluentAssertions;
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
                QueryWordMatch(2, FieldMatch(2, 1, 2), FieldMatch(4, 1)),
                QueryWordMatch(4, FieldMatch(3, 3), FieldMatch(4, 44, 99), FieldMatch(5, 2)));

            var sut = new FieldFilterQueryOperator("Test", 4, new ExactWordQueryPart("x"));

            var results = sut.Evaluate(() => navigator, QueryContext.Empty);

            results.Matches.Should().BeEquivalentTo(
                    QueryWordMatch(2, FieldMatch(4, 1)),
                    QueryWordMatch(4, FieldMatch(4, 44, 99))
                );
        }
    }
}

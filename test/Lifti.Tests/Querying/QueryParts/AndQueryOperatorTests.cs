using FluentAssertions;
using Lifti.Querying.QueryParts;
using System.Linq;
using Xunit;

namespace Lifti.Tests.Querying.QueryParts
{
    public class AndQueryOperatorTests : QueryTestBase
    {
        [Fact]
        public void ShouldOnlyReturnItemsAppearingOnBothSides()
        {
            var op = new AndQueryOperator(
                new FakeQueryPart(5, 8, 9),
                new FakeQueryPart(2, 5, 9));

            var result = op.Evaluate(() => new FakeIndexNavigator(), QueryContext.Empty);

            result.Matches.Select(m => m.ItemId).Should().BeEquivalentTo(
                new[] { 5, 9 });
        }

        [Fact]
        public void ShouldMergeIndexedWordsInCorrectOrderForMatchingFields()
        {
            var op = new AndQueryOperator(
                new FakeQueryPart(ScoredToken(5, ScoredFieldMatch(1D, 1, 1, 7), ScoredFieldMatch(3D, 2, 9, 20))),
                new FakeQueryPart(ScoredToken(5, ScoredFieldMatch(2D, 1, 9), ScoredFieldMatch(4D, 2, 3, 34))));

            var result = op.Evaluate(() => new FakeIndexNavigator(), QueryContext.Empty);

            result.Matches.Should().BeEquivalentTo(new[] {
                ScoredToken(
                    5,
                    ScoredFieldMatch(3D, 1, 1, 7, 9),
                    ScoredFieldMatch(7D, 2, 3, 9, 20, 34))});
        }
    }
}

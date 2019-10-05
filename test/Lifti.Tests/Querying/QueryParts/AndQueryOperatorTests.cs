using FluentAssertions;
using Lifti.Querying;
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

            var result = op.Evaluate(() => new FakeIndexNavigator());

            result.Matches.Select(m => m.ItemId).Should().BeEquivalentTo(
                new[] { 5, 9 });
        }

        [Fact]
        public void ShouldMergeIndexedWordsInCorrectOrderForMatchingFields()
        {
            var op = new AndQueryOperator(
                new FakeQueryPart(QueryWordMatch(5, FieldMatch(1, 1, 7), FieldMatch(2, 9, 20))),
                new FakeQueryPart(QueryWordMatch(5, FieldMatch(1, 9), FieldMatch(2, 3, 34))));

            var result = op.Evaluate(() => new FakeIndexNavigator());

            result.Matches.Should().BeEquivalentTo(new[] { 
                QueryWordMatch(
                    5,
                    FieldMatch(1, 1, 7, 9),
                    FieldMatch(2, 3, 9, 20, 34))});
        }
    }
}

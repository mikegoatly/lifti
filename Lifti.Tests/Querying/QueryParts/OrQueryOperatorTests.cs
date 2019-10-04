using FluentAssertions;
using Lifti.Querying;
using Lifti.Querying.QueryParts;
using System.Linq;
using Xunit;

namespace Lifti.Tests.Querying.QueryParts
{
    public class OrQueryOperatorTests : QueryTestBase
    {
        [Fact]
        public void ShouldReturnItemsAppearingOnBothSides()
        {
            var op = new OrQueryOperator(
                new FakeQueryPart(5, 8, 9),
                new FakeQueryPart(2, 5, 9));

            var result = op.Evaluate(() => new FakeIndexNavigator());

            result.Matches.Select(m => m.ItemId).Should().BeEquivalentTo(
                new[] { 2, 5, 8, 9 });
        }

        [Fact]
        public void ShouldMergeAllFieldMatchesInCorrectWordOrder()
        {
            var op = new OrQueryOperator(
                new FakeQueryPart(
                    QueryWordMatch(4, FieldMatch(1, 5, 6) ),
                    QueryWordMatch(5, FieldMatch(1, 9, 11))),
                new FakeQueryPart(
                    QueryWordMatch(5, FieldMatch(1, 1, 103), FieldMatch(2, 2, 18)),
                    QueryWordMatch(7, FieldMatch(1, 18) )));

            var result = op.Evaluate(() => new FakeIndexNavigator());

            result.Matches.Should().BeEquivalentTo(
                new[] {
                    QueryWordMatch(4, FieldMatch(1, 5, 6)),
                    QueryWordMatch(5, FieldMatch(1, 1, 9, 11, 103), FieldMatch(2, 2, 18)),
                    QueryWordMatch(7, FieldMatch(1, 18))
                });
        }
    }
}

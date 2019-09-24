using FluentAssertions;
using Lifti.Querying;
using System.Linq;
using Xunit;

namespace Lifti.Tests.Querying
{
    public class AndQueryOperatorTests
    {
        [Fact]
        public void ShouldOnlyReturnItemsAppearingOnBothSides()
        {
            var op = new AndQueryOperator(
                new FakeQueryPart(5, 8, 9),
                new FakeQueryPart(2, 5, 9));

            var result = op.Evaluate(() => new FakeIndexNavigator());

            result.Matches.Select(m => m.itemId).Should().BeEquivalentTo(
                new[] { 5, 9 });
        }

        [Fact]
        public void ShouldMergeMatchingWordLocations()
        {
            var op = new AndQueryOperator(
                new FakeQueryPart(new[] { (5, new[] { new IndexedWordLocation(1, new[] { new Range(1, 7) }) }) }),
                new FakeQueryPart(new[] { (5, new[] { new IndexedWordLocation(2, new[] { new Range(4, 6) }) }) }));

            var result = op.Evaluate(() => new FakeIndexNavigator());

            result.Matches.Should().BeEquivalentTo(
                new[] {
                    (5,
                    new[] 
                    {
                        new IndexedWordLocation(1, new[] { new Range(1, 7) }),
                        new IndexedWordLocation(2, new[] { new Range(4, 6) })
                    })
                });
        }
    }
}

using FluentAssertions;
using Lifti.Querying;
using System.Linq;
using Xunit;

namespace Lifti.Tests.Querying
{
    public class OrQueryOperatorTests
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
        public void ShouldMergeMatchingWordLocations()
        {
            var word1 = new FieldMatch(1, new[] { new WordLocation(0, 4, 3) });
            var word2 = new FieldMatch(1, new[] { new WordLocation(1, 1, 7) });
            var word3 = new FieldMatch(2, new[] { new WordLocation(2, 4, 6) });
            var word4 = new FieldMatch(1, new[] { new WordLocation(3, 2, 9) });

            var op = new OrQueryOperator(
                new FakeQueryPart(
                    new QueryWordMatch(4, new[] { word1 }),
                    new QueryWordMatch(5, new[] { word2 })),
                new FakeQueryPart(
                    new QueryWordMatch(5, new[] { word3 }),
                    new QueryWordMatch(7, new[] { word4 })));

            var result = op.Evaluate(() => new FakeIndexNavigator());

            result.Matches.Should().BeEquivalentTo(
                new[] {
                    new QueryWordMatch(4, new[] { word1 }),
                    new QueryWordMatch(5, new[] { word2, word3 }),
                    new QueryWordMatch(7, new[] { word4 }),
                });
        }
    }
}

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

            result.Matches.Select(m => m.ItemId).Should().BeEquivalentTo(
                new[] { 5, 9 });
        }

        [Fact]
        public void ShouldMergeIndexedWordsForMatchingFields()
        {
            var word1 = new FieldMatch(1, new[] { new WordLocation(1, 1, 7) });
            var word2 = new FieldMatch(2, new[] { new WordLocation(2, 4, 6) });
            var op = new AndQueryOperator(
                new FakeQueryPart(new QueryWordMatch(5, new[] { word1 })),
                new FakeQueryPart(new QueryWordMatch(5, new[] { word2 })));

            var result = op.Evaluate(() => new FakeIndexNavigator());

            result.Matches.Should().BeEquivalentTo(new[] { new QueryWordMatch(5, new[] { word1, word2 }) });
        }
    }
}

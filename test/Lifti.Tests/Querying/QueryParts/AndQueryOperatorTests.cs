using FluentAssertions;
using Lifti.Querying;
using Lifti.Querying.QueryParts;
using System;
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

            result.Matches.Select(m => m.DocumentId).Should().BeEquivalentTo(
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

        [Fact]
        public void CombineAll_WithEmptyElementSet_ShouldThrowException()
        {
            Assert.Throws<QueryParserException>(() => AndQueryOperator.CombineAll(Array.Empty<IQueryPart>()));
        }

        [Fact]
        public void CombineAll_WithSingleElement_ShouldReturnElement()
        {
            var op = AndQueryOperator.CombineAll(new[] { new ExactWordQueryPart("test") });

            op.ToString().Should().Be("test");
        }

        [Fact]
        public void CombineAll_WithMultipleElements_ShouldReturnElementCombinedWithOrStatement()
        {
            var op = AndQueryOperator.CombineAll(new[] { new ExactWordQueryPart("test"), new ExactWordQueryPart("test2"), new ExactWordQueryPart("test3") });

            op.ToString().Should().Be("test & test2 & test3");
        }

        [Fact]
        public void CalculateWeighting_ShouldReturnSmallestWeightingOfParts()
        {
            var op = new AndQueryOperator(new FakeQueryPart(2D), new FakeQueryPart(3D));

            op.CalculateWeighting(() => new FakeIndexNavigator()).Should().Be(2D);
        }
    }
}

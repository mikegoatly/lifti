using FluentAssertions;
using Lifti.Querying;
using Lifti.Querying.QueryParts;
using System;
using System.Linq;
using Xunit;

namespace Lifti.Tests.Querying.QueryParts
{
    public class AndNotQueryOperatorTests : QueryTestBase
    {
        [Fact]
        public void ShouldReturnItemsOnLeftButNotOnRight()
        {
            var op = new AndNotQueryOperator(
                new FakeQueryPart(2, 5, 8, 9),
                new FakeQueryPart(5, 9));

            var result = op.Evaluate(() => new FakeIndexNavigator(), QueryContext.Empty);

            result.Matches.Select(m => m.DocumentId).Should().BeEquivalentTo(
                new[] { 2, 8 });
        }

        [Fact]
        public void WhenLeftSideEmpty_ShouldReturnEmpty()
        {
            var op = new AndNotQueryOperator(
                new FakeQueryPart(Array.Empty<int>()),
                new FakeQueryPart(2, 5, 9));

            var result = op.Evaluate(() => new FakeIndexNavigator(), QueryContext.Empty);

            result.Matches.Should().BeEmpty();
        }

        [Fact]
        public void WhenRightSideEmpty_ShouldReturnAllLeftItems()
        {
            var op = new AndNotQueryOperator(
                new FakeQueryPart(2, 5, 8, 9),
                new FakeQueryPart(Array.Empty<int>()));

            var result = op.Evaluate(() => new FakeIndexNavigator(), QueryContext.Empty);

            result.Matches.Select(m => m.DocumentId).Should().BeEquivalentTo(
                new[] { 2, 5, 8, 9 });
        }

        [Fact]
        public void WhenNoOverlap_ShouldReturnAllLeftItems()
        {
            var op = new AndNotQueryOperator(
                new FakeQueryPart(1, 2, 3),
                new FakeQueryPart(4, 5, 6));

            var result = op.Evaluate(() => new FakeIndexNavigator(), QueryContext.Empty);

            result.Matches.Select(m => m.DocumentId).Should().BeEquivalentTo(
                new[] { 1, 2, 3 });
        }

        [Fact]
        public void WhenCompleteOverlap_ShouldReturnEmpty()
        {
            var op = new AndNotQueryOperator(
                new FakeQueryPart(2, 5, 9),
                new FakeQueryPart(2, 5, 9));

            var result = op.Evaluate(() => new FakeIndexNavigator(), QueryContext.Empty);

            result.Matches.Should().BeEmpty();
        }

        [Fact]
        public void ShouldPreserveScoresAndFieldMatchesFromLeftSide()
        {
            var op = new AndNotQueryOperator(
                new FakeQueryPart(
                    ScoredToken(5, ScoredFieldMatch(1D, 1, 1, 7), ScoredFieldMatch(3D, 2, 9, 20)),
                    ScoredToken(8, ScoredFieldMatch(2D, 1, 5))),
                new FakeQueryPart(5));

            var result = op.Evaluate(() => new FakeIndexNavigator(), QueryContext.Empty);

            result.Matches.Should().BeEquivalentTo(new[] {
                ScoredToken(8, ScoredFieldMatch(2D, 1, 5))
            });
        }

        [Fact]
        public void ShouldHandleMultipleFieldMatches()
        {
            var op = new AndNotQueryOperator(
                new FakeQueryPart(
                    ScoredToken(3, ScoredFieldMatch(1D, 1, 1), ScoredFieldMatch(2D, 2, 5)),
                    ScoredToken(5, ScoredFieldMatch(3D, 1, 7)),
                    ScoredToken(7, ScoredFieldMatch(4D, 2, 9))),
                new FakeQueryPart(5));

            var result = op.Evaluate(() => new FakeIndexNavigator(), QueryContext.Empty);

            result.Matches.Should().BeEquivalentTo(new[] {
                ScoredToken(3, ScoredFieldMatch(1D, 1, 1), ScoredFieldMatch(2D, 2, 5)),
                ScoredToken(7, ScoredFieldMatch(4D, 2, 9))
            });
        }

        [Fact]
        public void CalculateWeighting_ShouldReturnSmallestWeightingOfParts()
        {
            var op = new AndNotQueryOperator(new FakeQueryPart(2D), new FakeQueryPart(3D));

            op.CalculateWeighting(() => new FakeIndexNavigator()).Should().Be(2D);
        }

        [Fact]
        public void ToString_ShouldFormatCorrectly()
        {
            var op = new AndNotQueryOperator(
                new ExactWordQueryPart("eiffel"),
                new ExactWordQueryPart("tower"));

            op.ToString().Should().Be("eiffel &! tower");
        }

        [Fact]
        public void Precedence_ShouldBeOrPrecedence()
        {
            var op = new AndNotQueryOperator(
                new ExactWordQueryPart("test"),
                new ExactWordQueryPart("test2"));

            op.Precedence.Should().Be(OperatorPrecedence.Or);
        }
    }
}

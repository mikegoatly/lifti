using FluentAssertions;
using Lifti.Querying;
using Lifti.Querying.QueryParts;
using Xunit;

namespace Lifti.Tests.Querying.QueryParts
{
    public class AdjacentWordsQueryOperatorTests : QueryTestBase
    {
        [Fact]
        public void ShouldOnlyReturnMatchesForAppropriateField()
        {
            var sut = new AdjacentWordsQueryOperator(
                new[] {
                    new FakeQueryPart(
                        ScoredToken(7, ScoredFieldMatch(1D, 1, 8, 20, 100), ScoredFieldMatch(100D, 2, 9, 14)),
                        ScoredToken(8, ScoredFieldMatch(2D, 1, 11, 101), ScoredFieldMatch(101D, 2, 8, 104))),
                    new FakeQueryPart(
                        ScoredToken(7, ScoredFieldMatch(3D, 1, 7, 9, 21)),
                        ScoredToken(8, ScoredFieldMatch(4D, 1, 5, 102), ScoredFieldMatch(102D, 2, 9))),
                    new FakeQueryPart(
                        ScoredToken(7, ScoredFieldMatch(5D, 1, 8, 10)),
                        ScoredToken(8, ScoredFieldMatch(6D, 1, 103, 104), ScoredFieldMatch(103D, 2, 10)))
                    });

            var results = sut.Evaluate(() => new FakeIndexNavigator(), QueryContext.Empty);

            // Item 7 matches:
            // Field 1: ((8, 9), 10)
            // Field 2: None
            // Item 8 matches:
            // Field 1: ((101, 102), 103)
            // Field 2: ((8, 9), 10)
            results.Matches.Should().BeEquivalentTo(
                new[]
                {
                    ScoredToken(
                        7,
                        ScoredFieldMatch(9D, 1, CompositeMatch(8, 9, 10))),
                    ScoredToken(
                        8,
                        ScoredFieldMatch(12D, 1, CompositeMatch(101, 102, 103)),
                        ScoredFieldMatch(306D, 2, CompositeMatch(8, 9, 10)))
                },
                config => config.AllowingInfiniteRecursion());
        }

        [Fact]
        public void ShouldNotCombineSameTokensTogether()
        {
            var sut = new AdjacentWordsQueryOperator(
                new[] {
                    new FakeQueryPart(
                        ScoredToken(7, ScoredFieldMatch(1D, 1, 8, 20, 100))),
                    new FakeQueryPart(
                        ScoredToken(7, ScoredFieldMatch(1D, 1, 8, 20, 100)))
                    });

            var results = sut.Evaluate(() => new FakeIndexNavigator(), QueryContext.Empty);

            // Item 7 matches only:
            // Field 1: ((8, 9), 10)
            // The first and second query parts should not combine together
            results.Matches.Should().BeEmpty();
        }
    }
}

using FluentAssertions;
using Lifti.Querying;
using Xunit;

namespace Lifti.Tests.Querying
{
    public class PrecedingIntersectMergerTests : QueryTestBase
    {
        private static readonly ScoredToken[] expectedResults =
            [
                ScoredToken(
                    7,
                    ScoredFieldMatch(5D, 1, 34, 35, 99, 100),
                    ScoredFieldMatch(7D, 2, 3, 4)),
                ScoredToken(
                    8,
                    ScoredFieldMatch(10D, 2, 80, 85))
            ];

        [Fact]
        public void ForMatchingItemsAndFields_ShouldOnlyReturnWordsWhereTheEarliestLeftWordIsBeforeTheWordsOnTheRight()
        {
            var left = IntermediateQueryResult(
                ScoredToken(
                    7,
                    ScoredFieldMatch(1D, 1, 34, 99, 104, 320),
                    ScoredFieldMatch(2D, 2, 3)),
                ScoredToken(
                    8,
                    ScoredFieldMatch(3D, 2, 80, 91)));

            var right = IntermediateQueryResult(
                ScoredToken(
                    7,
                    ScoredFieldMatch(4D, 1, 3, 9, 35, 100),
                    ScoredFieldMatch(5D, 2, 1, 2, 4)),
                ScoredToken(
                    8,
                    ScoredFieldMatch(6D, 1, 8, 9),
                    ScoredFieldMatch(7D, 2, 3, 85)));

            var result = PrecedingIntersectMerger.Apply(left, right);

            result.Should().BeEquivalentTo(expectedResults);
        }

        [Fact]
        public void MoreMatchesOnLeft_ShouldNotAffectResults()
        {
            var left = IntermediateQueryResult(
                ScoredToken(
                    7,
                    ScoredFieldMatch(1D, 1, 34, 99, 104, 320),
                    ScoredFieldMatch(2D, 2, 3)),
                ScoredToken(
                    8,
                    ScoredFieldMatch(3D, 2, 80, 91)),
                ScoredToken(
                    9,
                    ScoredFieldMatch(1D, 1, 45, 100)));

            var right = IntermediateQueryResult(
                ScoredToken(
                    7,
                    ScoredFieldMatch(4D, 1, 3, 9, 35, 100),
                    ScoredFieldMatch(5D, 2, 1, 2, 4)),
                ScoredToken(
                    8,
                    ScoredFieldMatch(6D, 1, 8, 9),
                    ScoredFieldMatch(7D, 2, 3, 85)));

            var result = PrecedingIntersectMerger.Apply(left, right);

            result.Should().BeEquivalentTo(expectedResults);
        }

        [Fact]
        public void MoreMatchesOnRight_ShouldNotAffectResults()
        {
            var left = IntermediateQueryResult(
                ScoredToken(
                    7,
                    ScoredFieldMatch(1D, 1, 34, 99, 104, 320),
                    ScoredFieldMatch(2D, 2, 3)),
                ScoredToken(
                    8,
                    ScoredFieldMatch(3D, 2, 80, 91)));

            var right = IntermediateQueryResult(
                ScoredToken(
                    7,
                    ScoredFieldMatch(4D, 1, 3, 9, 35, 100),
                    ScoredFieldMatch(5D, 2, 1, 2, 4)),
                ScoredToken(
                    8,
                    ScoredFieldMatch(6D, 1, 8, 9),
                    ScoredFieldMatch(7D, 2, 3, 85)),
                ScoredToken(
                    9,
                    ScoredFieldMatch(1D, 1, 45, 100)));

            var result = PrecedingIntersectMerger.Apply(left, right);

            result.Should().BeEquivalentTo(expectedResults);
        }
    }
}

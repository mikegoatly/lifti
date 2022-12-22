using FluentAssertions;
using Lifti.Querying;
using Xunit;

namespace Lifti.Tests.Querying
{
    public class UnionMergerTests : QueryTestBase
    {
        [Fact]
        public void ShouldReturnMergedMatchesInWordIndexOrder()
        {
            var left = IntermediateQueryResult(ScoredToken(7, ScoredFieldMatch(1D, 1, 30, 41)));
            var right = IntermediateQueryResult(ScoredToken(7, ScoredFieldMatch(2D, 1, 35, 37, 42)));
            var result = UnionMerger.Apply(left, right);

            result.Should().BeEquivalentTo(new[]
            {
                ScoredToken(
                    7,
                    ScoredFieldMatch(3D, 1, 30, 35, 37, 41, 42))
            });
        }

        [Fact]
        public void LeftOrRightOrderShouldNotMatter()
        {
            var left = IntermediateQueryResult(
                ScoredToken(1, ScoredFieldMatch(1D, 1, 30)),
                ScoredToken(6, ScoredFieldMatch(2D, 1, 60)),
                ScoredToken(9, ScoredFieldMatch(3D, 1, 10)));
            var right = IntermediateQueryResult(
                ScoredToken(6, ScoredFieldMatch(4D, 1, 20)),
                ScoredToken(9, ScoredFieldMatch(5D, 1, 80)));

            var leftRightResult = UnionMerger.Apply(left, right);
            var rightLeftResult = UnionMerger.Apply(right, left);

            leftRightResult.Should().BeEquivalentTo(
                new[]
                {
                    ScoredToken(1, ScoredFieldMatch(1D, 1, 30)),
                    ScoredToken(6, ScoredFieldMatch(6D, 1, 20, 60)),
                    ScoredToken(9, ScoredFieldMatch(8D, 1, 10, 80))
                });

            leftRightResult.Should().BeEquivalentTo(rightLeftResult);
        }
    }
}

using FluentAssertions;
using Lifti.Querying;
using Xunit;

namespace Lifti.Tests.Querying
{
    public class ExceptMergerTests : QueryTestBase
    {
        [Fact]
        public void ShouldReturnItemsInLeftButNotRight()
        {
            var left = IntermediateQueryResult(
                ScoredToken(1, ScoredFieldMatch(1D, 1, 10)),
                ScoredToken(5, ScoredFieldMatch(2D, 1, 20)),
                ScoredToken(7, ScoredFieldMatch(3D, 1, 30)),
                ScoredToken(9, ScoredFieldMatch(4D, 1, 40)));
            var right = IntermediateQueryResult(
                ScoredToken(5, ScoredFieldMatch(5D, 1, 50)),
                ScoredToken(9, ScoredFieldMatch(6D, 1, 60)));

            var result = ExceptMerger.Apply(left, right);

            result.Should().BeEquivalentTo(
                new[]
                {
                    ScoredToken(1, ScoredFieldMatch(1D, 1, 10)),
                    ScoredToken(7, ScoredFieldMatch(3D, 1, 30))
                });
        }

        [Fact]
        public void WhenLeftEmpty_ShouldReturnEmpty()
        {
            var left = IntermediateQueryResult();
            var right = IntermediateQueryResult(
                ScoredToken(1, ScoredFieldMatch(1D, 1, 10)),
                ScoredToken(5, ScoredFieldMatch(2D, 1, 20)));

            var result = ExceptMerger.Apply(left, right);

            result.Should().BeEmpty();
        }

        [Fact]
        public void WhenRightEmpty_ShouldReturnAllLeft()
        {
            var left = IntermediateQueryResult(
                ScoredToken(1, ScoredFieldMatch(1D, 1, 10)),
                ScoredToken(5, ScoredFieldMatch(2D, 1, 20)),
                ScoredToken(9, ScoredFieldMatch(3D, 1, 30)));
            var right = IntermediateQueryResult();

            var result = ExceptMerger.Apply(left, right);

            result.Should().BeEquivalentTo(
                new[]
                {
                    ScoredToken(1, ScoredFieldMatch(1D, 1, 10)),
                    ScoredToken(5, ScoredFieldMatch(2D, 1, 20)),
                    ScoredToken(9, ScoredFieldMatch(3D, 1, 30))
                });
        }

        [Fact]
        public void WhenNoOverlap_ShouldReturnAllLeft()
        {
            var left = IntermediateQueryResult(
                ScoredToken(1, ScoredFieldMatch(1D, 1, 10)),
                ScoredToken(3, ScoredFieldMatch(2D, 1, 20)),
                ScoredToken(5, ScoredFieldMatch(3D, 1, 30)));
            var right = IntermediateQueryResult(
                ScoredToken(7, ScoredFieldMatch(4D, 1, 40)),
                ScoredToken(9, ScoredFieldMatch(5D, 1, 50)));

            var result = ExceptMerger.Apply(left, right);

            result.Should().BeEquivalentTo(
                new[]
                {
                    ScoredToken(1, ScoredFieldMatch(1D, 1, 10)),
                    ScoredToken(3, ScoredFieldMatch(2D, 1, 20)),
                    ScoredToken(5, ScoredFieldMatch(3D, 1, 30))
                });
        }

        [Fact]
        public void WhenCompleteOverlap_ShouldReturnEmpty()
        {
            var left = IntermediateQueryResult(
                ScoredToken(1, ScoredFieldMatch(1D, 1, 10)),
                ScoredToken(5, ScoredFieldMatch(2D, 1, 20)),
                ScoredToken(9, ScoredFieldMatch(3D, 1, 30)));
            var right = IntermediateQueryResult(
                ScoredToken(1, ScoredFieldMatch(4D, 1, 40)),
                ScoredToken(5, ScoredFieldMatch(5D, 1, 50)),
                ScoredToken(9, ScoredFieldMatch(6D, 1, 60)));

            var result = ExceptMerger.Apply(left, right);

            result.Should().BeEmpty();
        }

        [Fact]
        public void ShouldPreserveFieldMatchesAndScoresFromLeft()
        {
            var left = IntermediateQueryResult(
                ScoredToken(3, ScoredFieldMatch(1D, 1, 1), ScoredFieldMatch(2D, 2, 5)),
                ScoredToken(5, ScoredFieldMatch(3D, 1, 7), ScoredFieldMatch(4D, 2, 9)),
                ScoredToken(7, ScoredFieldMatch(5D, 1, 11)));
            var right = IntermediateQueryResult(
                ScoredToken(5, ScoredFieldMatch(99D, 1, 999)));

            var result = ExceptMerger.Apply(left, right);

            result.Should().BeEquivalentTo(
                new[]
                {
                    ScoredToken(3, ScoredFieldMatch(1D, 1, 1), ScoredFieldMatch(2D, 2, 5)),
                    ScoredToken(7, ScoredFieldMatch(5D, 1, 11))
                });
        }

        [Fact]
        public void ShouldHandleRightHavingMoreDocumentsThanLeft()
        {
            var left = IntermediateQueryResult(
                ScoredToken(2, ScoredFieldMatch(1D, 1, 10)),
                ScoredToken(5, ScoredFieldMatch(2D, 1, 20)));
            var right = IntermediateQueryResult(
                ScoredToken(1, ScoredFieldMatch(3D, 1, 30)),
                ScoredToken(2, ScoredFieldMatch(4D, 1, 40)),
                ScoredToken(3, ScoredFieldMatch(5D, 1, 50)),
                ScoredToken(5, ScoredFieldMatch(6D, 1, 60)),
                ScoredToken(7, ScoredFieldMatch(7D, 1, 70)),
                ScoredToken(9, ScoredFieldMatch(8D, 1, 80)));

            var result = ExceptMerger.Apply(left, right);

            result.Should().BeEmpty();
        }

        [Fact]
        public void ShouldHandleInterleavedDocumentIds()
        {
            var left = IntermediateQueryResult(
                ScoredToken(1, ScoredFieldMatch(1D, 1, 10)),
                ScoredToken(3, ScoredFieldMatch(2D, 1, 20)),
                ScoredToken(5, ScoredFieldMatch(3D, 1, 30)),
                ScoredToken(7, ScoredFieldMatch(4D, 1, 40)),
                ScoredToken(9, ScoredFieldMatch(5D, 1, 50)));
            var right = IntermediateQueryResult(
                ScoredToken(2, ScoredFieldMatch(6D, 1, 60)),
                ScoredToken(3, ScoredFieldMatch(7D, 1, 70)),
                ScoredToken(6, ScoredFieldMatch(8D, 1, 80)),
                ScoredToken(7, ScoredFieldMatch(9D, 1, 90)));

            var result = ExceptMerger.Apply(left, right);

            result.Should().BeEquivalentTo(
                new[]
                {
                    ScoredToken(1, ScoredFieldMatch(1D, 1, 10)),
                    ScoredToken(5, ScoredFieldMatch(3D, 1, 30)),
                    ScoredToken(9, ScoredFieldMatch(5D, 1, 50))
                });
        }

        [Fact]
        public void OrderMatters_LeftRightIsNotSameAsRightLeft()
        {
            var left = IntermediateQueryResult(
                ScoredToken(1, ScoredFieldMatch(1D, 1, 10)),
                ScoredToken(5, ScoredFieldMatch(2D, 1, 20)),
                ScoredToken(9, ScoredFieldMatch(3D, 1, 30)));
            var right = IntermediateQueryResult(
                ScoredToken(5, ScoredFieldMatch(4D, 1, 40)),
                ScoredToken(7, ScoredFieldMatch(5D, 1, 50)));

            var leftMinusRight = ExceptMerger.Apply(left, right);
            var rightMinusLeft = ExceptMerger.Apply(right, left);

            // Left - Right = {1, 9}
            leftMinusRight.Should().BeEquivalentTo(
                new[]
                {
                    ScoredToken(1, ScoredFieldMatch(1D, 1, 10)),
                    ScoredToken(9, ScoredFieldMatch(3D, 1, 30))
                });

            // Right - Left = {7}
            rightMinusLeft.Should().BeEquivalentTo(
                new[]
                {
                    ScoredToken(7, ScoredFieldMatch(5D, 1, 50))
                });

            // They should be different
            leftMinusRight.Should().NotBeEquivalentTo(rightMinusLeft);
        }
    }
}

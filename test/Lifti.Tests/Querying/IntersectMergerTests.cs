using FluentAssertions;
using Lifti.Querying;
using Xunit;

namespace Lifti.Tests.Querying
{
    public class IntersectMergerTests : QueryTestBase
    {
        [Fact]
        public void ShouldReturnMergedMatchesInWordIndexOrder()
        {
            var left = IntermediateQueryResult(QueryWordMatch(7, FieldMatch(1, 30, 41)));
            var right = IntermediateQueryResult(QueryWordMatch(7, FieldMatch(1, 35, 37, 42)));
            var result = IntersectMerger.Instance.Apply(left, right);

            result.Should().BeEquivalentTo(new[]
            {
                QueryWordMatch(
                    7,
                    FieldMatch(1, 30, 35, 37, 41, 42))
            });
        }

        [Fact]
        public void LeftOrRightOrderShouldNotMatter()
        {
            var left = IntermediateQueryResult(
                QueryWordMatch(1, FieldMatch(1, 30)),
                QueryWordMatch(6, FieldMatch(1, 60)),
                QueryWordMatch(9, FieldMatch(1, 10)));
            var right = IntermediateQueryResult(
                QueryWordMatch(6, FieldMatch(1, 20)),
                QueryWordMatch(9, FieldMatch(1, 80)));

            var leftRightResult = IntersectMerger.Instance.Apply(left, right);
            var rightLeftResult = IntersectMerger.Instance.Apply(right, left);

            leftRightResult.Should().BeEquivalentTo(
                QueryWordMatch(6, FieldMatch(1, 20, 60)),
                QueryWordMatch(9, FieldMatch(1, 10, 80)));

            leftRightResult.Should().BeEquivalentTo(rightLeftResult);
        }
    }
}

using FluentAssertions;
using Lifti.Querying;
using Xunit;

namespace Lifti.Tests.Querying
{
    public class PrecedingIntersectMergerTests : QueryTestBase
    {
        [Fact]
        public void ForMatchingItemsAndFields_ShouldOnlyReturnWordsWhereTheEarliestLeftWordIsBeforeTheWordsOnTheRight()
        {
            var left = IntermediateQueryResult(
                QueryWordMatch(
                    7,
                    FieldMatch(1, 34, 99, 104, 320),
                    FieldMatch(2, 3)),
                QueryWordMatch(
                    8,
                    FieldMatch(2, 80, 91)));

            var right = IntermediateQueryResult(
                QueryWordMatch(
                    7,
                    FieldMatch(1, 3, 9, 35, 100),
                    FieldMatch(2, 1, 2, 4)),
                QueryWordMatch(
                    8,
                    FieldMatch(1, 8, 9),
                    FieldMatch(2, 3, 85)));

            var result = PrecedingIntersectMerger.Instance.Apply(left, right);

            result.Should().BeEquivalentTo(new[]
            {
                QueryWordMatch(
                    7,
                    FieldMatch(1, 34, 35, 99, 100),
                    FieldMatch(2, 3, 4)),
                QueryWordMatch(
                    8,
                    FieldMatch(2, 80, 85))
            });
        }
    }
}

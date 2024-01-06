using FluentAssertions;
using Xunit;

namespace Lifti.Tests.Querying
{
    public class IntermediateQueryResultTests : QueryTestBase
    {
        [Fact]
        public void Equals_ReturnsTrueForMatchingData()
        {
            var a = IntermediateQueryResult(ScoredToken(10, ScoredFieldMatch(2D, 1, 1, 2, 3)));
            var b = IntermediateQueryResult(ScoredToken(10, ScoredFieldMatch(2D, 1, 1, 2, 3)));

            a.Equals(b).Should().BeTrue();
        }

        [Fact]
        public void Equals_ReturnsFalseForNonMatchingData()
        {
            var a = IntermediateQueryResult(ScoredToken(10, ScoredFieldMatch(2D, 1, 1, 2, 3)));

            // Differs by locations
            a.Equals(IntermediateQueryResult(ScoredToken(10, ScoredFieldMatch(2D, 1, 1, 2, 4)))).Should().BeFalse();

            // Differs by score
            a.Equals(IntermediateQueryResult(ScoredToken(10, ScoredFieldMatch(9D, 1, 1, 2, 3)))).Should().BeFalse();

            // Differs by field id
            a.Equals(IntermediateQueryResult(ScoredToken(10, ScoredFieldMatch(2D, 2, 1, 2, 3)))).Should().BeFalse();

            // Differs by document id
            a.Equals(IntermediateQueryResult(ScoredToken(1, ScoredFieldMatch(2D, 1, 1, 2, 3)))).Should().BeFalse();
        }
    }
}

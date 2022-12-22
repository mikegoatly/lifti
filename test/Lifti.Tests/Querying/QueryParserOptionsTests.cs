using FluentAssertions;
using Lifti.Querying;
using Xunit;

namespace Lifti.Tests.Querying
{
    public class QueryParserOptionsTests
    {
        [Theory]
        [InlineData(6, 2)]
        [InlineData(3, 1)]
        [InlineData(1, 0)]
        public void DefaultMaxEditDistance_ShouldCalculateExpectedValues(int termLength, ushort expectedResult)
        {
            new QueryParserOptions().FuzzySearchMaxEditDistance(termLength).Should().Be(expectedResult);
        }

        [Theory]
        [InlineData(16, 4)]
        [InlineData(8, 2)]
        [InlineData(6, 1)]
        [InlineData(4, 1)]
        [InlineData(1, 1)]
        public void DefaultMaxSequentialEditDistance_ShouldCalculateExpectedValues(int termLength, ushort expectedResult)
        {
            new QueryParserOptions().FuzzySearchMaxSequentialEdits(termLength).Should().Be(expectedResult);
        }
    }
}

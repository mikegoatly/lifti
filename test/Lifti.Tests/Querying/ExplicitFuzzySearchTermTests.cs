using FluentAssertions;
using Lifti.Querying;
using Xunit;

namespace Lifti.Tests.Querying
{
    public class ExplicitFuzzySearchTermTests
    {
        [Fact]
        public void ForNonFuzzySearchTerm_ShouldReturnNoMatch()
        {
            var result = ExplicitFuzzySearchTerm.Parse("test");
            result.IsFuzzyMatch.Should().BeFalse();
        }

        [Fact]
        public void FuzzySearchTermWithoutParameters_ShouldSuccessfullyMatch()
        {
            var result = ExplicitFuzzySearchTerm.Parse("?test");
            result.Should().BeEquivalentTo(
                new ExplicitFuzzySearchTerm(true, 1, null, null));
        }

        [Fact]
        public void FuzzySearchTermWithEmptyParameters_ShouldSuccessfullyMatch()
        {
            var result = ExplicitFuzzySearchTerm.Parse("?,?test");
            result.Should().BeEquivalentTo(
                new ExplicitFuzzySearchTerm(true, 3, null, null));
        }

        [Fact]
        public void FuzzySearchTermWithMaxEditDistanceOnly_ShouldSuccessfullyMatch()
        {
            var result = ExplicitFuzzySearchTerm.Parse("?4,?test");
            result.Should().BeEquivalentTo(
                new ExplicitFuzzySearchTerm(true, 4, 4, null));
        }

        [Fact]
        public void FuzzySearchTermWithMaxSequentialOnly_ShouldSuccessfullyMatch()
        {
            var result = ExplicitFuzzySearchTerm.Parse("?,2?test");
            result.Should().BeEquivalentTo(
                new ExplicitFuzzySearchTerm(true, 4, null, 2));
        }

        [Theory]
        [InlineData(0, 0, 5)]
        [InlineData(2, 5, 5)]
        [InlineData(20, 50, 7)]
        [InlineData(200, 500, 9)]
        public void FuzzySearchTermWithBothParameters_ShouldSuccessfullyMatch(ushort maxEdits, ushort maxSequentialEdits, int expectedTokenStart)
        {
            var result = ExplicitFuzzySearchTerm.Parse($"?{maxEdits},{maxSequentialEdits}?test");
            result.Should().BeEquivalentTo(
                new ExplicitFuzzySearchTerm(true, expectedTokenStart, maxEdits, maxSequentialEdits));
        }

        [Fact]
        public void FuzzySearchTermWithOutOfRangeMaxEdits_ShouldThrowException()
        {
            Assert.Throws<QueryParserException>(() => ExplicitFuzzySearchTerm.Parse($"?65536,2?test"));
        }

        [Fact]
        public void FuzzySearchTermWithOutOfRangeMaxSequentialEdits_ShouldThrowException()
        {
            Assert.Throws<QueryParserException>(() => ExplicitFuzzySearchTerm.Parse($"?2,65536?test"));
        }
    }
}

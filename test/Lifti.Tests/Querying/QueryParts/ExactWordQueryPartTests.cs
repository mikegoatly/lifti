using FluentAssertions;
using Lifti.Querying;
using Lifti.Querying.QueryParts;
using Lifti.Tests.Fakes;
using Xunit;

namespace Lifti.Tests.Querying.QueryParts
{
    public class ExactWordQueryPartTests
    {
        [Fact]
        public void Evaluating_ShouldNavigateThroughTextAndGetAllDirectMatches()
        {
            var part = new ExactWordQueryPart("test");
            var navigator = FakeIndexNavigator.ReturningExactMatches(1, 2);

            var actual = part.Evaluate(() => navigator, QueryContext.Empty);

            actual.Should().BeEquivalentTo(navigator.ExpectedExactMatches);
            navigator.NavigatedStrings.Should().BeEquivalentTo(["test"]);
            navigator.NavigatedCharacters.Should().BeEmpty();
            navigator.ProvidedWeightings.Should().BeEquivalentTo(new[] { 1D });
        }

        [Fact]
        public void Evaluating_ShouldPassThroughScoreBoostToNavigator()
        {
            var part = new ExactWordQueryPart("test", 5D);
            var navigator = FakeIndexNavigator.ReturningExactMatches(1, 2);

            var actual = part.Evaluate(() => navigator, QueryContext.Empty);

            actual.Should().BeEquivalentTo(navigator.ExpectedExactMatches);
            navigator.NavigatedStrings.Should().BeEquivalentTo(["test"]);
            navigator.NavigatedCharacters.Should().BeEmpty();
            navigator.ProvidedWeightings.Should().BeEquivalentTo(new[] { 5D });
        }

        [Fact]
        public void ShouldApplyQueryContextToResults()
        {
            var part = new ExactWordQueryPart("test");
            var navigator = FakeIndexNavigator.ReturningExactMatches(1, 2);

            var contextResults = new IntermediateQueryResult();
            var queryContext = new QueryContext();
            var result = part.Evaluate(() => new FakeIndexNavigator(), queryContext);

            result.Should().Be(contextResults);
        }

        [Fact]
        public void ToString_ShouldReturnCorrectRepresentation()
        {
            var part = new ExactWordQueryPart("test");
            part.ToString().Should().Be("test");
        }

        [Fact]
        public void ToString_WithScoreBoost_ShouldReturnCorrectRepresentation()
        {
            var part = new ExactWordQueryPart("test", 5.123);
            part.ToString().Should().Be("test^5.123");
        }
    }
}

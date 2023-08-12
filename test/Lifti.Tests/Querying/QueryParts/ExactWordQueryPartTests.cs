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
            navigator.NavigatedStrings.Should().BeEquivalentTo(new[] { "test" });
            navigator.NavigatedCharacters.Should().BeEmpty();
        }

        [Fact]
        public void ShouldApplyQueryContextToResults()
        {
            var part = new ExactWordQueryPart("test");
            var navigator = FakeIndexNavigator.ReturningExactMatches(1, 2);

            var contextResults = new IntermediateQueryResult();
            var queryContext = new FakeQueryContext(contextResults);
            var result = part.Evaluate(() => new FakeIndexNavigator(), queryContext);

            result.Should().Be(contextResults);
        }
    }
}

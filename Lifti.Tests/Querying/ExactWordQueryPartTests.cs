using FluentAssertions;
using Lifti.Querying;
using Xunit;

namespace Lifti.Tests.Querying
{
    public class ExactWordQueryPartTests
    {
        [Fact]
        public void Evaluating_ShouldNavigateThroughTextAndGetAllDirectMatches()
        {
            var part = new ExactWordQueryPart("test");
            var navigator = FakeIndexNavigator.ReturningExactMatches(1, 2);

            var actual = part.Evaluate(() => navigator);

            actual.Should().BeEquivalentTo(navigator.ExpectedExactMatches);
            navigator.NavigatedStrings.Should().BeEquivalentTo(new[] { "test" });
            navigator.NavigatedCharacters.Should().BeEmpty();
        }
    }
}

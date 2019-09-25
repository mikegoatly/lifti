using FluentAssertions;
using Lifti.Querying;
using Xunit;

namespace Lifti.Tests.Querying
{
    public class StartsWithWordQueryPartTests
    {
        [Fact]
        public void Evaluating_ShouldNavigateThroughTextAndGetAllDirectAndChildMatches()
        {
            var part = new StartsWithWordQueryPart("test");
            var navigator = FakeIndexNavigator.ReturningExactAndChildMatches(1, 2);

            var actual = part.Evaluate(() => navigator);

            actual.Should().BeEquivalentTo(navigator.ExpectedExactAndChildMatches);
            navigator.NavigatedStrings.Should().BeEquivalentTo(new[] { "test" });
            navigator.NavigatedCharacters.Should().BeEmpty();
        }
    }
}

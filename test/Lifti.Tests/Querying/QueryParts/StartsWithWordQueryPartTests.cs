using FluentAssertions;
using Lifti.Querying.QueryParts;
using System.Linq;
using Xunit;

namespace Lifti.Tests.Querying.QueryParts
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

        //[Fact]
        //public void Evaluating_ShouldAlwaysReturnWordsInAscendingOrder()
        //{
        //    var part = new StartsWithWordQueryPart("test");
        //    var navigator = FakeIndexNavigator.ReturningExactAndChildMatches(11, 2, 9, 1);

        //    var actual = part.Evaluate(() => navigator);

        //    foreach (var itemMatch in actual.Matches)
        //    {
        //        foreach (var fieldMatch in itemMatch.FieldMatches)
        //        {
        //            fieldMatch.GetWordLocations().Select(l => l.WordIndex).Should().BeInAscendingOrder();
        //        }
        //    }
        //}
    }
}

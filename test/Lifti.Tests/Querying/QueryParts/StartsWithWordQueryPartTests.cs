using FluentAssertions;
using Lifti.Querying;
using Lifti.Querying.QueryParts;
using Moq;
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

            var actual = part.Evaluate(() => navigator, QueryContext.Empty);

            actual.Should().BeEquivalentTo(navigator.ExpectedExactAndChildMatches);
            navigator.NavigatedStrings.Should().BeEquivalentTo(new[] { "test" });
            navigator.NavigatedCharacters.Should().BeEmpty();
        }

        [Fact]
        public void ShouldApplyQueryContextToResults()
        {
            var part = new StartsWithWordQueryPart("test");
            var navigator = FakeIndexNavigator.ReturningExactMatches(1, 2);

            var contextResults = new IntermediateQueryResult();
            var queryContext = new Mock<IQueryContext>();
            queryContext.Setup(c => c.ApplyTo(It.IsAny<IntermediateQueryResult>())).Returns(contextResults);
            var result = part.Evaluate(() => new FakeIndexNavigator(), queryContext.Object);

            result.Should().Be(contextResults);
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

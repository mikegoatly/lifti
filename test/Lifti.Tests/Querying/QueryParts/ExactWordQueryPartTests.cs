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

        [Fact]
        public void CalculateWeighting_ShouldReturnWeightingBasedOnNumberOfMatchedDocuments()
        {
            var navigator = FakeIndexNavigator.ReturningExactMatches(1, 2);
            navigator.Snapshot = new FakeIndexSnapshot(new FakeIndexMetadata<int>(10));
            var part = new ExactWordQueryPart("test", 5.123);

            // 2 matches out of 10 documents results in a weighting of 0.2
            part.CalculateWeighting(() => navigator).Should().Be(0.2D);

            navigator.Snapshot = new FakeIndexSnapshot(new FakeIndexMetadata<int>(2));
            part = new ExactWordQueryPart("test", 5.123);

            // 2 matches out of 2 documents results in a weighting of 1
            part.CalculateWeighting(() => navigator).Should().Be(1D);
        }

        [Fact]
        public void CalculateWeighting_ShouldCacheWeighting()
        {
            var navigator = FakeIndexNavigator.ReturningExactMatches(1, 2);
            navigator.Snapshot = new FakeIndexSnapshot(new FakeIndexMetadata<int>(10));
            var part = new ExactWordQueryPart("test", 5.123);
            part.CalculateWeighting(() => navigator).Should().Be(0.2D);

            // Changing the snapshot is a hacky way of checking that the score is cached - 
            // if it isn't, the weighting will be recalculated and will be different
            navigator.Snapshot = new FakeIndexSnapshot(new FakeIndexMetadata<int>(2));
            part.CalculateWeighting(() => navigator).Should().Be(0.2D);
        }
    }

    internal class FakeIndexSnapshot : IIndexSnapshot
    {
        public FakeIndexSnapshot(IIndexMetadata indexMetadata)
        {
            this.Metadata = indexMetadata;
        }

        public IndexNode Root => throw new System.NotImplementedException();

        public IIndexedFieldLookup FieldLookup => throw new System.NotImplementedException();

        public IIndexMetadata Items => this.Metadata;

        public IIndexMetadata Metadata { get; private set; }

        public IIndexNavigator CreateNavigator()
        {
            throw new System.NotImplementedException();
        }
    }
}

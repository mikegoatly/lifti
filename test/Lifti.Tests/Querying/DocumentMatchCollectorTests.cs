using FluentAssertions;
using Lifti.Querying;
using Xunit;

namespace Lifti.Tests.Querying
{
    public class DocumentMatchCollectorTests : QueryTestBase
    {
        [Fact]
        public void AddingSingleDocument()
        {
            var sut = new DocumentMatchCollector();

            sut.Add(10, 1, TokenLocations(1, 2, 3), 2D);

            sut.ToIntermediateQueryResult()
                .Should().BeEquivalentTo(
                    IntermediateQueryResult(ScoredToken(10, ScoredFieldMatch(2D, 1, 1, 2, 3))),
                    o => o.WithStrictOrdering());
        }

        [Fact]
        public void AddingDifferentDocuments()
        {
            var sut = new DocumentMatchCollector();

            sut.Add(10, 1, TokenLocations(1, 2, 3), 2D);
            sut.Add(1, 1, TokenLocations(1, 2, 3), 2D);

            sut.ToIntermediateQueryResult()
                .Should().BeEquivalentTo(
                    IntermediateQueryResult(
                        ScoredToken(1, ScoredFieldMatch(2D, 1, 1, 2, 3)),
                        ScoredToken(10, ScoredFieldMatch(2D, 1, 1, 2, 3))),
                    o => o.WithStrictOrdering());
        }

        [Fact]
        public void AddingDifferentFieldForSameDocument()
        {
            var sut = new DocumentMatchCollector();

            sut.Add(10, 1, TokenLocations(1, 2, 3), 2D);
            sut.Add(10, 2, TokenLocations(2), 20D);

            sut.ToIntermediateQueryResult()
                .Should().BeEquivalentTo(
                    IntermediateQueryResult(
                        ScoredToken(10, 
                            ScoredFieldMatch(2D, 1, 1, 2, 3),
                            ScoredFieldMatch(20D, 2, 2))),
                    o => o.WithStrictOrdering());
        }

        [Fact]
        public void AddingMoreLocationsForSameField_ShouldMergeFieldsAndAddScores()
        {
            var sut = new DocumentMatchCollector();

            sut.Add(10, 1, TokenLocations(1, 2, 30), 2D);
            sut.Add(10, 1, TokenLocations(9), 20D);

            sut.ToIntermediateQueryResult()
                .Should().BeEquivalentTo(
                    IntermediateQueryResult(
                        ScoredToken(10,
                            ScoredFieldMatch(22D, 1, 1, 2, 9, 30))),
                    o => o.WithStrictOrdering());
        }
    }
}

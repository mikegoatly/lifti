using FluentAssertions;
using Lifti.Querying;
using System.Linq;
using Xunit;

namespace Lifti.Tests.Querying
{
    public class CompositePositionalIntersectMergerTests : QueryTestBase
    {
        private static readonly IntermediateQueryResult exactMatchLeft = IntermediateQueryResult(
                QueryWordMatch(
                    7,
                    FieldMatch(1, 30, 100),
                    FieldMatch(2, 5)),
                QueryWordMatch(
                    8,
                    FieldMatch(2, 80)));

        private static readonly IntermediateQueryResult exactMatchRight = IntermediateQueryResult(
                QueryWordMatch(
                    7,
                    FieldMatch(1, 35, 95),
                    FieldMatch(2, 10)),
                QueryWordMatch(
                    8,
                    FieldMatch(2, 75)));

        [Fact]
        public void ShouldReturnMatchesInMinWordIndexOrder()
        {
            var left = IntermediateQueryResult(QueryWordMatch(7, FieldMatch(1, 30, 41)));
            var right = IntermediateQueryResult(QueryWordMatch(7, FieldMatch(1, 35, 37, 42)));
            var result = CompositePositionalIntersectMerger.Instance.Apply(left, right, 5, 5);

            var matchedLocations = result.SelectMany(r => r.FieldMatches.SelectMany(m => m.Locations)).ToList();
            matchedLocations.Should().HaveCount(3);
            matchedLocations.Select(l => l.MinWordIndex)
                .Should().BeInAscendingOrder();
        }

        [Fact]
        public void WhenExactToleranceMatches_ShouldReturnCompositeMatch()
        {
            var left = IntermediateQueryResult(QueryWordMatch(7, FieldMatch(1, 30)));
            var right = IntermediateQueryResult(QueryWordMatch(7, FieldMatch(1, 35)));
            var result = CompositePositionalIntersectMerger.Instance.Apply(left, right, 0, 5);

            result.Should().BeEquivalentTo(new[]
            {
                QueryWordMatch(
                    7,
                    FieldMatch(1, (30, 35)))
            });
        }

        [Fact]
        public void WhenExactRightToleranceMatchesOnlyForSomeFields_ShouldOnlyReturnCompositeMatchesForCorrectWords()
        {
            var result = CompositePositionalIntersectMerger.Instance.Apply(exactMatchLeft, exactMatchRight, 0, 5);

            result.Should().BeEquivalentTo(new[]
            {
                QueryWordMatch(
                    7,
                    FieldMatch(1, (30, 35)),
                    FieldMatch(2, (5, 10)))
            });
        }

        [Fact]
        public void WhenExactLeftToleranceMatchesOnlyForSomeFields_ShouldOnlyReturnCompositeMatchesForCorrectWords()
        {
            var result = CompositePositionalIntersectMerger.Instance.Apply(exactMatchLeft, exactMatchRight, 5, 0);

            result.Should().BeEquivalentTo(new[]
            {
                QueryWordMatch(
                    7,
                    FieldMatch(1, (100, 95))),
                QueryWordMatch(
                    8,
                    FieldMatch(2, (80, 75)))
            });
        }

        [Fact]
        public void WhenExactToleranceMatchesOnlyForSomeFields_ShouldOnlyReturnCompositeMatchesForCorrectWords()
        {
            var result = CompositePositionalIntersectMerger.Instance.Apply(exactMatchLeft, exactMatchRight, 5, 5);

            result.Should().BeEquivalentTo(new[]
            {
                QueryWordMatch(
                    7,
                    FieldMatch(1, (30, 35), (100, 95)),
                    FieldMatch(2, (5, 10))),
                QueryWordMatch(
                    8,
                    FieldMatch(2, (80, 75)))
            });
        }

        [Fact]
        public void WhenJustOutsideTolerance_ShouldReturnNothing()
        {
            var left = IntermediateQueryResult(QueryWordMatch(7, FieldMatch(1, 30)));
            var right = IntermediateQueryResult(QueryWordMatch(7, FieldMatch(1, 36)));
            var result = CompositePositionalIntersectMerger.Instance.Apply(left, right, 5, 5);

            result.Should().BeEmpty();
        }
    }
}

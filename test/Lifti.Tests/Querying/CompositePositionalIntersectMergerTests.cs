using FluentAssertions;
using Lifti.Querying;
using System.Linq;
using Xunit;

namespace Lifti.Tests.Querying
{
    public class CompositePositionalIntersectMergerTests : QueryTestBase
    {
        private static readonly IntermediateQueryResult exactMatchLeft = IntermediateQueryResult(
                ScoredToken(
                    7,
                    ScoredFieldMatch(1D, 1, 30, 100),
                    ScoredFieldMatch(1D, 2, 5)),
                ScoredToken(
                    8,
                    ScoredFieldMatch(1D, 2, 80)));

        private static readonly IntermediateQueryResult exactMatchRight = IntermediateQueryResult(
                ScoredToken(
                    7,
                    ScoredFieldMatch(1D, 1, 35, 95),
                    ScoredFieldMatch(3D, 2, 10)),
                ScoredToken(
                    8,
                    ScoredFieldMatch(5D, 2, 75)));

        [Fact]
        public void ShouldReturnMatchesInMinTokenIndexOrder()
        {
            var left = IntermediateQueryResult(ScoredToken(7, ScoredFieldMatch(1D, 1, 30, 41)));
            var right = IntermediateQueryResult(ScoredToken(7, ScoredFieldMatch(1D, 1, 35, 37, 42)));
            var result = CompositePositionalIntersectMerger.Instance.Apply(left, right, 5, 5);

            var matchedLocations = result.SelectMany(r => r.FieldMatches.SelectMany(m => m.Locations)).ToList();
            matchedLocations.Should().HaveCount(3);
            matchedLocations.Select(l => l.MinTokenIndex)
                .Should().BeInAscendingOrder();
        }

        [Fact]
        public void WhenExactToleranceMatches_ShouldReturnCompositeMatch()
        {
            var left = IntermediateQueryResult(ScoredToken(7, ScoredFieldMatch(1D, 1, 30)));
            var right = IntermediateQueryResult(ScoredToken(7, ScoredFieldMatch(1D, 1, 35)));
            var result = CompositePositionalIntersectMerger.Instance.Apply(left, right, 0, 5);

            result.Should().BeEquivalentTo(
                ScoredToken(7,ScoredFieldMatch(2D, 1, (30, 35)))
            );
        }

        [Fact]
        public void WhenExactRightToleranceMatchesOnlyForSomeFields_ShouldOnlyReturnCompositeMatchesForCorrectTokens()
        {
            var result = CompositePositionalIntersectMerger.Instance.Apply(exactMatchLeft, exactMatchRight, 0, 5);

            result.Should().BeEquivalentTo(
                ScoredToken(
                    7,
                    ScoredFieldMatch(2D, 1, (30, 35)),
                    ScoredFieldMatch(4D, 2, (5, 10))));
        }

        [Fact]
        public void WhenExactLeftToleranceMatchesOnlyForSomeFields_ShouldOnlyReturnCompositeMatchesForCorrectTokens()
        {
            var result = CompositePositionalIntersectMerger.Instance.Apply(exactMatchLeft, exactMatchRight, 5, 0);

            result.Should().BeEquivalentTo(new[]
            {
                ScoredToken(
                    7,
                    ScoredFieldMatch(2D, 1, (100, 95))),
                ScoredToken(
                    8,
                    ScoredFieldMatch(6D, 2, (80, 75)))
            });
        }

        [Fact]
        public void WhenExactToleranceMatchesOnlyForSomeFields_ShouldOnlyReturnCompositeMatchesForCorrectTokens()
        {
            var result = CompositePositionalIntersectMerger.Instance.Apply(exactMatchLeft, exactMatchRight, 5, 5);

            result.Should().BeEquivalentTo(new[]
            {
                ScoredToken(
                    7,
                    ScoredFieldMatch(2D, 1, (30, 35), (100, 95)),
                    ScoredFieldMatch(4D, 2, (5, 10))),
                ScoredToken(
                    8,
                    ScoredFieldMatch(6D, 2, (80, 75)))
            });
        }

        [Fact]
        public void WhenJustOutsideTolerance_ShouldReturnNothing()
        {
            var left = IntermediateQueryResult(ScoredToken(7, ScoredFieldMatch(1D, 1, 30)));
            var right = IntermediateQueryResult(ScoredToken(7, ScoredFieldMatch(1D, 1, 36)));
            var result = CompositePositionalIntersectMerger.Instance.Apply(left, right, 5, 5);

            result.Should().BeEmpty();
        }
    }
}

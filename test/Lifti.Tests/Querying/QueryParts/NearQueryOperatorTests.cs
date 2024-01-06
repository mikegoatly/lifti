﻿using FluentAssertions;
using Lifti.Querying;
using Lifti.Querying.QueryParts;
using Xunit;

namespace Lifti.Tests.Querying.QueryParts
{
    public class NearQueryOperatorTests : QueryTestBase
    {
        [Fact]
        public void ShouldOnlyReturnMatchesForAppropriateField()
        {
            var sut = new NearQueryOperator(
                new FakeQueryPart(
                    ScoredToken(7, ScoredFieldMatch(1D, 1, 8, 20, 100), ScoredFieldMatch(5D, 2, 9, 14)),
                    ScoredToken(8, ScoredFieldMatch(2D, 1, 11, 101), ScoredFieldMatch(6D, 2, 8, 104))),
                new FakeQueryPart(
                    ScoredToken(7, ScoredFieldMatch(3D, 1, 6, 14, 102)),
                    ScoredToken(8, ScoredFieldMatch(4D, 1, 5, 106), ScoredFieldMatch(7D, 2, 3, 105))));

            var results = sut.Evaluate(() => new FakeIndexNavigator(), QueryContext.Empty);

            // Item 7 matches:
            // Field 1: (8, 6) (100, 102)
            // Field 2: None
            // Item 8 matches:
            // Field 1: (101, 106)
            // Field 2: (8, 3) (104, 105)
            results.Matches.Should().BeEquivalentTo(new[]
            {
                ScoredToken(
                    7,
                    ScoredFieldMatch(4D, 1, CompositeTokenLocation(8, 6), CompositeTokenLocation(100, 102))),
                ScoredToken(
                    8,
                    ScoredFieldMatch(6D, 1, CompositeTokenLocation(101, 106)),
                    ScoredFieldMatch(13D, 2, CompositeTokenLocation(8, 3), CompositeTokenLocation(104, 105)))
            });
        }

        [Fact]
        public void CalculateWeighting_ShouldReturnSmallestWeightingOfParts()
        {
            var op = new NearQueryOperator(new FakeQueryPart(3D), new FakeQueryPart(2D));

            op.CalculateWeighting(() => new FakeIndexNavigator()).Should().Be(2D);
        }
    }
}

﻿using FluentAssertions;
using Lifti.Querying;
using Lifti.Querying.QueryParts;
using System.Linq;
using Xunit;

namespace Lifti.Tests.Querying.QueryParts
{
    public class OrQueryOperatorTests : QueryTestBase
    {
        [Fact]
        public void ShouldReturnItemsAppearingOnBothSides()
        {
            var op = new OrQueryOperator(
                new FakeQueryPart(5, 8, 9),
                new FakeQueryPart(2, 5, 9));

            var result = op.Evaluate(() => new FakeIndexNavigator(), QueryContext.Empty);

            result.Matches.Select(m => m.ItemId).Should().BeEquivalentTo(
                new[] { 2, 5, 8, 9 });
        }

        [Fact]
        public void ShouldMergeAllFieldMatchesInCorrectWordOrder()
        {
            var op = new OrQueryOperator(
                new FakeQueryPart(
                    ScoredToken(4, ScoredFieldMatch(1D, 1, 5, 6) ),
                    ScoredToken(5, ScoredFieldMatch(2D, 1, 9, 11))),
                new FakeQueryPart(
                    ScoredToken(5, ScoredFieldMatch(3D, 1, 1, 103), ScoredFieldMatch(9D, 2, 2, 18)),
                    ScoredToken(7, ScoredFieldMatch(4D, 1, 18) )));

            var result = op.Evaluate(() => new FakeIndexNavigator(), QueryContext.Empty);

            result.Matches.Should().BeEquivalentTo(
                new[] {
                    ScoredToken(4, ScoredFieldMatch(1D, 1, 5, 6)),
                    ScoredToken(5, ScoredFieldMatch(5D, 1, 1, 9, 11, 103), ScoredFieldMatch(9D, 2, 2, 18)),
                    ScoredToken(7, ScoredFieldMatch(4D, 1, 18))
                });
        }
    }
}

using FluentAssertions;
using Lifti.Querying;
using Lifti.Querying.QueryParts;
using Xunit;

namespace Lifti.Tests.Querying.QueryParts
{
    public class PrecedingQueryOperatorTests : OperatorTestBase
    {
        [Fact]
        public void ShouldOnlyReturnMatchesForAppropriateField()
        {
            var sut = new PrecedingQueryOperator(
                new FakeQueryPart(
                    new QueryWordMatch(7, FieldMatch(1, 8, 20, 100)),
                    new QueryWordMatch(7, FieldMatch(2, 9, 14)),
                    new QueryWordMatch(8, FieldMatch(1, 11, 101)),
                    new QueryWordMatch(8, FieldMatch(2, 8, 104))),
                new FakeQueryPart(
                    new QueryWordMatch(7, FieldMatch(1, 6, 14, 102)),
                    new QueryWordMatch(8, FieldMatch(1, 5, 106)),
                    new QueryWordMatch(8, FieldMatch(2, 3, 105))));

            var results = sut.Evaluate(() => new FakeIndexNavigator());

            // Item 7 matches:
            // Field 1: 8, 14, 102, 20, 100
            // Field 2: None
            // Item 8 matches:
            // Field 1: 11, 106, 101
            // Field 2: 8, 105, 104
            results.Matches.Should().BeEquivalentTo(
                new QueryWordMatch(
                    7,
                    new FieldMatch(1, WordMatch(8), WordMatch(14), WordMatch(102), WordMatch(20), WordMatch(100))),
                new QueryWordMatch(
                    8,
                    new FieldMatch(1, WordMatch(11), WordMatch(106), WordMatch(101)),
                    new FieldMatch(2, WordMatch(8), WordMatch(105), WordMatch(104))));
        }
    }
}

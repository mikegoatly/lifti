using FluentAssertions;
using Lifti.Querying;
using Lifti.Querying.QueryParts;
using Xunit;

namespace Lifti.Tests.Querying.QueryParts
{
    public class PrecedingQueryOperatorTests : QueryTestBase
    {
        [Fact]
        public void ShouldOnlyReturnMatchesForAppropriateField()
        {
            var sut = new PrecedingQueryOperator(
                new FakeQueryPart(
                    QueryWordMatch(7, FieldMatch(1, 8, 20, 100), FieldMatch(2, 9, 14)),
                    QueryWordMatch(8, FieldMatch(1, 11, 101), FieldMatch(2, 8, 104))),
                new FakeQueryPart(
                    QueryWordMatch(7, FieldMatch(1, 6, 14, 102)),
                    QueryWordMatch(8, FieldMatch(1, 5, 106), FieldMatch(2, 3, 105))));

            var results = sut.Evaluate(() => new FakeIndexNavigator(), QueryContext.Empty);

            // Item 7 matches:
            // Field 1: 8, 14, 102, 20, 100
            // Field 2: None
            // Item 8 matches:
            // Field 1: 11, 106, 101
            // Field 2: 8, 105, 104
            results.Matches.Should().BeEquivalentTo(
                QueryWordMatch(
                    7,
                    new FieldMatch(1, WordMatch(8), WordMatch(14), WordMatch(20), WordMatch(100), WordMatch(102))),
                QueryWordMatch(
                    8,
                    new FieldMatch(1, WordMatch(11), WordMatch(101), WordMatch(106)),
                    new FieldMatch(2, WordMatch(8), WordMatch(104), WordMatch(105))));
        }
    }
}

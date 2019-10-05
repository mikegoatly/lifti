using FluentAssertions;
using Lifti.Querying;
using Lifti.Querying.QueryParts;
using Xunit;

namespace Lifti.Tests.Querying.QueryParts
{
    public class AdjacentWordsQueryOperatorTests : QueryTestBase
    {
        [Fact]
        public void ShouldOnlyReturnMatchesForAppropriateField()
        {
            var sut = new AdjacentWordsQueryOperator(
                new[] {
                    new FakeQueryPart(
                        QueryWordMatch(7, FieldMatch(1, 8, 20, 100), FieldMatch(2, 9, 14)),
                        QueryWordMatch(8, FieldMatch(1, 11, 101), FieldMatch(2, 8, 104))),
                    new FakeQueryPart(
                        QueryWordMatch(7, FieldMatch(1, 7, 9, 21)),
                        QueryWordMatch(8, FieldMatch(1, 5, 102), FieldMatch(2, 9))),
                    new FakeQueryPart(
                        QueryWordMatch(7, FieldMatch(1, 8, 10)),
                        QueryWordMatch(8, FieldMatch(1, 103, 104), FieldMatch(2, 10)))
                    });

            var results = sut.Evaluate(() => new FakeIndexNavigator());

            // Item 7 matches:
            // Field 1: ((8, 9), 10)
            // Field 2: None
            // Item 8 matches:
            // Field 1: ((101, 102), 103)
            // Field 2: ((8, 9), 10)
            results.Matches.Should().BeEquivalentTo(
                new[]
                {
                    QueryWordMatch(
                        7,
                        new FieldMatch(1, CompositeMatch(8, 9, 10))),
                    QueryWordMatch(
                        8,
                        new FieldMatch(1, CompositeMatch(101, 102, 103)),
                        new FieldMatch(2, CompositeMatch(8, 9, 10)))
                },
                config => config.AllowingInfiniteRecursion());
        }
    }
}

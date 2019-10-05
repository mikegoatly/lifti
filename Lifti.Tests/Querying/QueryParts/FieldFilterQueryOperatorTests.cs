using FluentAssertions;
using Lifti.Querying;
using Lifti.Querying.QueryParts;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Lifti.Tests.Querying.QueryParts
{
    public class FieldFilterQueryOperatorTests : QueryTestBase
    {
        [Fact]
        public void ShouldFilterAllItemResultsToRequiredField()
        {
            var sut = new FieldFilterQueryOperator(
                "Test", 4, new FakeQueryPart(
                    QueryWordMatch(2, FieldMatch(2, 1, 2), FieldMatch(4, 1)),
                    QueryWordMatch(4, FieldMatch(3, 3), FieldMatch(4, 44, 99), FieldMatch(5, 2))));

            sut.Evaluate(() => new FakeIndexNavigator()).Matches.Should().BeEquivalentTo(
                    QueryWordMatch(2, FieldMatch(4, 1)),
                    QueryWordMatch(4, FieldMatch(4, 44, 99))
                );
        }
    }
}

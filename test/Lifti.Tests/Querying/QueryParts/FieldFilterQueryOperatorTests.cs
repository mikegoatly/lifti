using FluentAssertions;
using Lifti.Querying;
using Lifti.Querying.QueryParts;
using Xunit;

namespace Lifti.Tests.Querying.QueryParts
{
    public class FieldFilterQueryOperatorTests : QueryTestBase
    {
        [Fact]
        public void ShouldPassFieldInQueryContext()
        {
            var navigator = new FakeIndexNavigator();

            var sut = new FieldFilterQueryOperator("Test", 4, new ExactWordQueryPart("x"));

            var results = sut.Evaluate(() => navigator, QueryContext.Empty);

            navigator.ProvidedQueryContexts.Should().BeEquivalentTo(
                [
                    new QueryContext(4)
                ]);
        }
    }
}

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

        [Fact]
        public void CalculateWeighting_ShouldReturnHalfOfChildPartWeighting()
        {
            var op = new FieldFilterQueryOperator("Field", 1, new FakeQueryPart(4D));

            op.CalculateWeighting(() => new FakeIndexNavigator()).Should().Be(2D);
        }

        [Theory]
        [InlineData("Field", "[Field]=FOO")]
        [InlineData("Field[1]", @"[Field\[1\]]=FOO")]
        [InlineData("Field name", @"[Field name]=FOO")]
        public void ToString_ShouldReturnExpectedString(string fieldName, string expectedResult)
        {
            var op = new FieldFilterQueryOperator(fieldName, 1, new ExactWordQueryPart("FOO"));

            op.ToString().Should().Be(expectedResult);
        }
    }
}

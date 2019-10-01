using FluentAssertions;
using Lifti.Querying.QueryParts;
using Xunit;

namespace Lifti.Tests.Querying.QueryParts
{
    public class Formatting
    {
        [Fact]
        public void QueryPartsShouldFormatToTextCorrectly()
        {
            var query = new BracketedQueryPart(
                new AndQueryOperator(
                    new OrQueryOperator(
                        new PrecedingNearQueryOperator(
                            new StartsWithWordQueryPart("test1"),
                            new ExactWordQueryPart("test2"),
                            2),
                        new PrecedingNearQueryOperator(
                            new StartsWithWordQueryPart("test3"),
                            new ExactWordQueryPart("test4"),
                            5)),
                    new PrecedingQueryOperator(
                        new NearQueryOperator(
                            new StartsWithWordQueryPart("test1"),
                            new ExactWordQueryPart("test2"),
                            2),
                        new NearQueryOperator(
                            new StartsWithWordQueryPart("test3"),
                            new ExactWordQueryPart("test4"),
                            5))));

            query.ToString().Should().Be("(test1* ~2> test2 | test3* ~> test4 & test1* ~2 test2 > test3* ~ test4)");
        }
    }
}

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
                            new WildcardQueryPart(WildcardQueryFragment.CreateText("test1"), WildcardQueryFragment.MultiCharacter),
                            new ExactWordQueryPart("test2"),
                            2),
                        new PrecedingNearQueryOperator(
                            new WildcardQueryPart(WildcardQueryFragment.SingleCharacter, WildcardQueryFragment.CreateText("test3"), WildcardQueryFragment.MultiCharacter),
                            new ExactWordQueryPart("test4"),
                            5)),
                    new PrecedingQueryOperator(
                        new NearQueryOperator(
                            new WildcardQueryPart(WildcardQueryFragment.CreateText("test1"), WildcardQueryFragment.MultiCharacter),
                            new ExactWordQueryPart("test2"),
                            2),
                        new NearQueryOperator(
                            new WildcardQueryPart(WildcardQueryFragment.CreateText("test3"), WildcardQueryFragment.MultiCharacter),
                            new ExactWordQueryPart("test4"),
                            5))));

            query.ToString().Should().Be("(test1* ~2> test2 | %test3* ~> test4 & test1* ~2 test2 > test3* ~ test4)");
        }
    }
}

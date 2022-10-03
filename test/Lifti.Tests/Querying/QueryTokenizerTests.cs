using FluentAssertions;
using Lifti.Querying;
using System;
using System.Linq;
using Xunit;

namespace Lifti.Tests.Querying
{
    public class QueryTokenizerTests
    {
        private readonly QueryTokenizer sut;

        public QueryTokenizerTests()
        {
            this.sut = new QueryTokenizer();
        }

        [Fact]
        public void EmptyStringYieldsNoResults()
        {
            this.sut.ParseQueryTokens(string.Empty).Should().BeEmpty();
        }

        [Fact]
        public void NullStringThrowsException()
        {
            Assert.Throws<ArgumentNullException>(() => this.sut.ParseQueryTokens(null).ToArray());
        }

        [Fact]
        public void SingleWordYieldsOneResult()
        {
            this.sut.ParseQueryTokens("Testing").Should().BeEquivalentTo(
                QueryToken.ForText("Testing"));
        }

        [Fact]
        public void FuzzySearchTermsYieldedCorrectly()
        {
            this.sut.ParseQueryTokens("?Testing ?1,2?Test ?,?Test").Should().BeEquivalentTo(
                QueryToken.ForText("?Testing"),
                QueryToken.ForText("?1,2?Test"),
                QueryToken.ForText("?,?Test"));
        }

        [Fact]
        public void FuzzySearchTermsSeparatedByCommasYieldedCorrectly()
        {
            this.sut.ParseQueryTokens("?Testing,?Test2").Should().BeEquivalentTo(
                QueryToken.ForText("?Testing"),
                QueryToken.ForText("?Test2"));
        }

        [Fact]
        public void EmptyFuzzySearchTermsShouldYieldNoTokens()
        {
            this.sut.ParseQueryTokens("? ?1,2? ?,? ?? ???").Should().BeEmpty();
        }

        [Fact]
        public void SingleWordWithSpacePaddingYieldsOneResult()
        {
            this.sut.ParseQueryTokens("  \t  Testing   \t ").Should().BeEquivalentTo(
                QueryToken.ForText("Testing"));
        }

        [Fact]
        public void CompositeStringYieldsOneResult()
        {
            this.sut.ParseQueryTokens("\"Jack be quick\"").Should().BeEquivalentTo(
                QueryToken.ForOperator(QueryTokenType.BeginAdjacentTextOperator),
                QueryToken.ForText("Jack"),
                QueryToken.ForText("be"),
                QueryToken.ForText("quick"),
                QueryToken.ForOperator(QueryTokenType.EndAdjacentTextOperator));
        }

        [Fact]
        public void TwoCompositeStringsYieldsSixResults()
        {
            this.sut.ParseQueryTokens(@"""First string"" ""Second string""").Should().BeEquivalentTo(
                QueryToken.ForOperator(QueryTokenType.BeginAdjacentTextOperator),
                QueryToken.ForText("First"),
                QueryToken.ForText("string"),
                QueryToken.ForOperator(QueryTokenType.EndAdjacentTextOperator),
                QueryToken.ForOperator(QueryTokenType.BeginAdjacentTextOperator),
                QueryToken.ForText("Second"),
                QueryToken.ForText("string"),
                QueryToken.ForOperator(QueryTokenType.EndAdjacentTextOperator));
        }

        [Fact]
        public void OperatorTokensAreParsedCorrectly()
        {
            this.sut.ParseQueryTokens(@"& ( | ) ~> ~2> > ~ ~2 test=").Should().BeEquivalentTo(
                QueryToken.ForOperator(QueryTokenType.AndOperator),
                QueryToken.ForOperator(QueryTokenType.OpenBracket),
                QueryToken.ForOperator(QueryTokenType.OrOperator),
                QueryToken.ForOperator(QueryTokenType.CloseBracket),
                QueryToken.ForOperatorWithTolerance(QueryTokenType.PrecedingNearOperator, 5),
                QueryToken.ForOperatorWithTolerance(QueryTokenType.PrecedingNearOperator, 2),
                QueryToken.ForOperator(QueryTokenType.PrecedingOperator),
                QueryToken.ForOperatorWithTolerance(QueryTokenType.NearOperator, 5),
                QueryToken.ForOperatorWithTolerance(QueryTokenType.NearOperator, 2),
                QueryToken.ForFieldFilter("test"));
        }

        [Fact]
        public void UnknownOperatorTokensRaiseExceptions()
        {
            Assert.Throws<QueryParserException>(() => this.sut.ParseQueryTokens(@"test =").ToArray())
                .Message.Should().Be("An unexpected operator was encountered: =");
        }
    }
}

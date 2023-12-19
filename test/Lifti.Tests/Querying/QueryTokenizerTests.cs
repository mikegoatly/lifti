using FluentAssertions;
using Lifti.Querying;
using Lifti.Tokenization;
using System;
using System.Linq;
using Xunit;

namespace Lifti.Tests.Querying
{
    public class QueryTokenizerTests
    {
        private readonly QueryTokenizer sut;
        private readonly IIndexTokenizer defaultIndexTokenizer;
        private readonly IIndexTokenizer fieldIndexTokenizer;
        private readonly FakeIndexTokenizer alternativeFieldIndexTokenizer;
        private readonly IIndexTokenizerProvider tokenizerProvider;

        public QueryTokenizerTests()
        {
            this.sut = new QueryTokenizer();

            this.defaultIndexTokenizer = new FakeIndexTokenizer();
            this.fieldIndexTokenizer = new FakeIndexTokenizer(true);
            this.alternativeFieldIndexTokenizer = new FakeIndexTokenizer(true);
            this.tokenizerProvider = new FakeIndexTokenizerProvider(
                this.defaultIndexTokenizer,
                ("test", this.fieldIndexTokenizer),
                ("test field", this.fieldIndexTokenizer),
                ("test []", this.alternativeFieldIndexTokenizer));
        }

        [Fact]
        public void EmptyStringYieldsNoResults()
        {
            this.sut.ParseQueryTokens(string.Empty, this.tokenizerProvider).Should().BeEmpty();
        }

        [Fact]
        public void NullStringThrowsException()
        {
            Assert.Throws<ArgumentNullException>(() => this.sut.ParseQueryTokens(null!, this.tokenizerProvider).ToArray());
        }

        [Fact]
        public void SingleWordYieldsOneResult()
        {
            this.sut.ParseQueryTokens("Testing", this.tokenizerProvider).Should().BeEquivalentTo(new[]
            {
                QueryToken.ForText("Testing", this.defaultIndexTokenizer, null)
            });
        }

        [Fact]
        public void ScoreBoostingOnlyAppliedToRequestedToken()
        {
            this.sut.ParseQueryTokens("(Testing^2) | Testing", this.tokenizerProvider).Should().BeEquivalentTo(new[]
            {
                QueryToken.ForOperator(QueryTokenType.OpenBracket),
                QueryToken.ForText("Testing", this.defaultIndexTokenizer, 2D),
                QueryToken.ForOperator(QueryTokenType.CloseBracket),
                QueryToken.ForOperator(QueryTokenType.OrOperator),
                QueryToken.ForText("Testing", this.defaultIndexTokenizer, null)
            });
        }

        [InlineData("2", 2D)]
        [InlineData("100.2927", 100.2927D)]
        [InlineData("001.001", 1.001D)]
        [Theory]
        public void SingleWordWithScoreBoostYieldsOneResult(string textScoreBoost, double expectedScoreBoost)
        {
            this.sut.ParseQueryTokens($"Testing^{textScoreBoost}", this.tokenizerProvider).Should().BeEquivalentTo(new[]
            {
                QueryToken.ForText("Testing", this.defaultIndexTokenizer, expectedScoreBoost)
            });
        }

        [Fact]
        public void FuzzySearchTermsYieldedCorrectly()
        {
            this.sut.ParseQueryTokens("?Testing ?1,2?Test ?,?Test", this.tokenizerProvider).Should().BeEquivalentTo(new[]
            {
                QueryToken.ForText("?Testing", this.defaultIndexTokenizer, null),
                QueryToken.ForText("?1,2?Test", this.defaultIndexTokenizer, null),
                QueryToken.ForText("?,?Test", this.defaultIndexTokenizer, null)
            });
        }

        [InlineData("(Testing & test))")]
        [InlineData(")")]
        [InlineData("((test) & test) & test)")]
        [Theory]
        public void UnexpectedCloseBracketShouldThrowException(string query)
        {
            Assert.Throws<QueryParserException>(() => this.sut.ParseQueryTokens(query, this.tokenizerProvider).ToList())
                .Message.Should().Be("Unexpected close bracket encountered in query");
        }

        [Fact]
        public void FuzzySearchTermsSeparatedByCommasYieldedCorrectly()
        {
            this.sut.ParseQueryTokens("?Testing,?Test2", this.tokenizerProvider).Should().BeEquivalentTo(
                new[]
                {
                    QueryToken.ForText("?Testing", this.defaultIndexTokenizer, null),
                    QueryToken.ForText("?Test2", this.defaultIndexTokenizer, null)
                });
        }

        [Fact]
        public void EscapedCharacters_ShouldBeReturnedAsTokenText()
        {
            this.sut.ParseQueryTokens(@"\\hello\=\"" \&\|", this.tokenizerProvider).Should().BeEquivalentTo(
                new[]
                {
                    QueryToken.ForText(@"\hello=""", this.defaultIndexTokenizer, null),
                    QueryToken.ForText("&|", this.defaultIndexTokenizer, null)
                });
        }

        [Fact]
        public void EmptyFuzzySearchTermsShouldYieldNoTokens()
        {
            this.sut.ParseQueryTokens("? ?1,2? ?,? ?? ???", this.tokenizerProvider).Should().BeEmpty();
        }

        [Fact]
        public void SingleWordWithSpacePaddingYieldsOneResult()
        {
            this.sut.ParseQueryTokens("  \t  Testing   \t ", this.tokenizerProvider).Should().BeEquivalentTo(new[]
            {
                QueryToken.ForText("Testing", this.defaultIndexTokenizer, null)
            });
        }

        [Fact]
        public void CompositeStringYieldsOneResult()
        {
            this.sut.ParseQueryTokens("\"Jack be quick\"", this.tokenizerProvider).Should().BeEquivalentTo(new[]
            {
                QueryToken.ForOperator(QueryTokenType.BeginAdjacentTextOperator),
                QueryToken.ForText("Jack", this.defaultIndexTokenizer, null),
                QueryToken.ForText("be", this.defaultIndexTokenizer, null),
                QueryToken.ForText("quick", this.defaultIndexTokenizer, null),
                QueryToken.ForOperator(QueryTokenType.EndAdjacentTextOperator)
            });
        }

        [Fact]
        public void TwoCompositeStringsYieldsSixResults()
        {
            this.sut.ParseQueryTokens(@"""First string"" ""Second string""", this.tokenizerProvider).Should().BeEquivalentTo(new[]
            {
                QueryToken.ForOperator(QueryTokenType.BeginAdjacentTextOperator),
                QueryToken.ForText("First", this.defaultIndexTokenizer, null),
                QueryToken.ForText("string", this.defaultIndexTokenizer, null),
                QueryToken.ForOperator(QueryTokenType.EndAdjacentTextOperator),
                QueryToken.ForOperator(QueryTokenType.BeginAdjacentTextOperator),
                QueryToken.ForText("Second", this.defaultIndexTokenizer, null),
                QueryToken.ForText("string", this.defaultIndexTokenizer, null),
                QueryToken.ForOperator(QueryTokenType.EndAdjacentTextOperator)
            });
        }

        [Fact]
        public void TextTokensForField_ShouldHaveCorrectTokenizerAssociatedToThem()
        {
            this.sut.ParseQueryTokens(@"test=""test string"" notfield test=sim%le nofield test=(yes* (""field too"") ?stillfield) notinfieldagain", this.tokenizerProvider).Should().BeEquivalentTo(new[]
                {
                    QueryToken.ForFieldFilter("test"),
                    QueryToken.ForOperator(QueryTokenType.BeginAdjacentTextOperator),
                    QueryToken.ForText("test", this.fieldIndexTokenizer, null),
                    QueryToken.ForText("string", this.fieldIndexTokenizer, null),
                    QueryToken.ForOperator(QueryTokenType.EndAdjacentTextOperator),
                    QueryToken.ForText("notfield", this.defaultIndexTokenizer, null),
                    QueryToken.ForFieldFilter("test"),
                    QueryToken.ForText("sim%le", this.fieldIndexTokenizer, null),
                    QueryToken.ForText("nofield", this.defaultIndexTokenizer, null),
                    QueryToken.ForFieldFilter("test"),
                    QueryToken.ForOperator(QueryTokenType.OpenBracket),
                    QueryToken.ForText("yes*", this.fieldIndexTokenizer, null),
                    QueryToken.ForOperator(QueryTokenType.OpenBracket),
                    QueryToken.ForOperator(QueryTokenType.BeginAdjacentTextOperator),
                    QueryToken.ForText("field", this.fieldIndexTokenizer, null),
                    QueryToken.ForText("too", this.fieldIndexTokenizer, null),
                    QueryToken.ForOperator(QueryTokenType.BeginAdjacentTextOperator),
                    QueryToken.ForOperator(QueryTokenType.CloseBracket),
                    QueryToken.ForText("?stillfield", this.fieldIndexTokenizer, null),
                    QueryToken.ForOperator(QueryTokenType.CloseBracket),
                    QueryToken.ForText("notinfieldagain", this.defaultIndexTokenizer, null)
                },
                options => options
                    .WithStrictOrdering()
                    .Using<QueryToken>(
                    x => x.Subject.IndexTokenizer.Should().BeSameAs(x.Expectation.IndexTokenizer))
                .WhenTypeIs<QueryToken>());
        }

        [Fact]
        public void BracketedFieldNames_ShouldBeTreatedAsFieldNameWithoutSquareBrackets()
        {
            this.sut.ParseQueryTokens(@"[test]=foo [test field]=bar", this.tokenizerProvider).Should().BeEquivalentTo(new[]
                {
                    QueryToken.ForFieldFilter("test"),
                    QueryToken.ForText("foo", this.fieldIndexTokenizer, null),
                    QueryToken.ForFieldFilter("test field"),
                    QueryToken.ForText("bar", this.fieldIndexTokenizer, null)
                },
                options => options
                    .WithStrictOrdering()
                    .Using<QueryToken>(
                    x => x.Subject.IndexTokenizer.Should().BeSameAs(x.Expectation.IndexTokenizer))
                .WhenTypeIs<QueryToken>());
        }

        [Fact]
        public void BracketedFieldNamesWithEscapedCharacters_ShouldReturnUnescapedCharacters()
        {
            this.sut.ParseQueryTokens(@"[\t\e\s\t\ \[\]]=foo", this.tokenizerProvider).Should().BeEquivalentTo(new[]
                {
                    QueryToken.ForFieldFilter("test []"),
                    QueryToken.ForText("foo", this.alternativeFieldIndexTokenizer, null)
                },
                options => options
                    .WithStrictOrdering()
                    .Using<QueryToken>(
                    x => x.Subject.IndexTokenizer.Should().BeSameAs(x.Expectation.IndexTokenizer))
                .WhenTypeIs<QueryToken>());
        }

        [Fact]
        public void EmptyBracketedFieldName_ShouldThrowException()
        {
            Assert.Throws<QueryParserException>(() => this.sut.ParseQueryTokens("[]=foo", this.tokenizerProvider).ToList())
                .Message.Should().Be("Empty field name encountered");
        }

        [Fact]
        public void UnclosedFieldNameBracket_ShouldThrowException()
        {
            Assert.Throws<QueryParserException>(() => this.sut.ParseQueryTokens("[test=foo", this.tokenizerProvider).ToList())
                .Message.Should().Be("Unclosed [ encountered");
        }

        [Fact]
        public void BracketFieldNameWithoutFollowingQuery_ShouldThrowException()
        {
            Assert.Throws<QueryParserException>(() => this.sut.ParseQueryTokens("[test] foo", this.tokenizerProvider).ToList())
                .Message.Should().Be("Expected = after bracketed field name");
        }

        [Fact]
        public void OperatorTokensAreParsedCorrectly()
        {
            this.sut.ParseQueryTokens(@"& ( | ) ~> ~2> > ~ ~2 test=", this.tokenizerProvider).Should().BeEquivalentTo(new[]
            {
                QueryToken.ForOperator(QueryTokenType.AndOperator),
                QueryToken.ForOperator(QueryTokenType.OpenBracket),
                QueryToken.ForOperator(QueryTokenType.OrOperator),
                QueryToken.ForOperator(QueryTokenType.CloseBracket),
                QueryToken.ForOperatorWithTolerance(QueryTokenType.PrecedingNearOperator, 5),
                QueryToken.ForOperatorWithTolerance(QueryTokenType.PrecedingNearOperator, 2),
                QueryToken.ForOperator(QueryTokenType.PrecedingOperator),
                QueryToken.ForOperatorWithTolerance(QueryTokenType.NearOperator, 5),
                QueryToken.ForOperatorWithTolerance(QueryTokenType.NearOperator, 2),
                QueryToken.ForFieldFilter("test")
            });
        }

        [Fact]
        public void UnknownOperatorTokensRaiseExceptions()
        {
            Assert.Throws<QueryParserException>(() => this.sut.ParseQueryTokens(@"test =", this.tokenizerProvider).ToArray())
                .Message.Should().Be("An unexpected operator was encountered: =");
        }
    }
}

using FluentAssertions;
using Lifti.Querying;
using Lifti.Querying.QueryParts;
using Lifti.Tokenization.TextExtraction;
using Moq;
using Xunit;

namespace Lifti.Tests.Querying
{
    public class QueryParserTests
    {
        private const byte TestFieldId = 9;
        private const byte OtherFieldId = 11;
        private readonly Mock<IIndexedFieldLookup> fieldLookupMock;

        public QueryParserTests()
        {
            this.fieldLookupMock = new Mock<IIndexedFieldLookup>();
            var testFieldId = TestFieldId;
            var otherFieldId = OtherFieldId;

            var tokenizer = new FakeTokenizer();
            var textExtractor = new PlainTextExtractor();
            this.fieldLookupMock.Setup(l => l.GetFieldInfo("testfield")).Returns(new IndexedFieldDetails(testFieldId, textExtractor, tokenizer));
            this.fieldLookupMock.Setup(l => l.GetFieldInfo("otherfield")).Returns(new IndexedFieldDetails(otherFieldId, textExtractor, tokenizer));
        }

        [Fact]
        public void ParsingTwoWordsWithNoOperator_ShouldComposeWithAndOperator()
        {
            var result = this.Parse("wordone wordtwo");
            var expectedQuery = new AndQueryOperator(new ExactWordQueryPart("wordone"), new ExactWordQueryPart("wordtwo"));
            VerifyResult(result, expectedQuery);
        }

        [Fact]
        public void ParsingTwoFuzzyWordsWithNoOperator_ShouldComposeWithAndOperator()
        {
            var result = this.Parse("?wordone ?wordtwo");
            var expectedQuery = new AndQueryOperator(new FuzzyMatchQueryPart("wordone", 3), new FuzzyMatchQueryPart("wordtwo", 3));
            VerifyResult(result, expectedQuery);
        }

        [Fact]
        public void ParsingMixOfWordMatchesWithNoOperator_ShouldComposeWithAndOperators()
        {
            var result = this.Parse("?wordone wordtwo wor* ?wordthree");
            var expectedQuery =
                new AndQueryOperator(
                    new AndQueryOperator(
                        new AndQueryOperator(
                            new FuzzyMatchQueryPart("wordone", 3), 
                            new ExactWordQueryPart("wordtwo")),
                        new WildcardQueryPart(WildcardQueryFragment.CreateText("wor"), WildcardQueryFragment.MultiCharacter)),
                    new FuzzyMatchQueryPart("wordthree",3));
            VerifyResult(result, expectedQuery);
        }

        [Fact]
        public void ParsingTwoWordsWithAndOperator_ShouldComposeWithAndOperator()
        {
            var result = this.Parse("wordone & wordtwo");
            var expectedQuery = new AndQueryOperator(new ExactWordQueryPart("wordone"), new ExactWordQueryPart("wordtwo"));
            VerifyResult(result, expectedQuery);
        }

        [Fact]
        public void ParsingTwoWordsWithPrecedingOperator_ShouldComposeWithPrecedingOperator()
        {
            var result = this.Parse("wordone > wordtwo");
            var expectedQuery = new PrecedingQueryOperator(new ExactWordQueryPart("wordone"), new ExactWordQueryPart("wordtwo"));
            VerifyResult(result, expectedQuery);
        }

        [Fact]
        public void ParsingSingleExactWord_ShouldReturnExactWordQueryPart()
        {
            var result = this.Parse("wordone");
            var expectedQuery = new ExactWordQueryPart("wordone");
            VerifyResult(result, expectedQuery);
        }

        [Fact]
        public void ParsingBracketedSingleExpression_ShouldReturnBracketedQueryPartContainer()
        {
            var result = this.Parse("(wordone*)");
            var expectedQuery = new BracketedQueryPart(
                new WildcardQueryPart(
                    WildcardQueryFragment.CreateText("wordone"), 
                    WildcardQueryFragment.MultiCharacter));

            VerifyResult(result, expectedQuery);
        }

        [Fact]
        public void ParsingBracketedAndExpressions_ShouldReturnBracketedQueryPartContainer()
        {
            var result = this.Parse("(wordone wordtwo)");
            var expectedQuery = new BracketedQueryPart(
                    new AndQueryOperator(new ExactWordQueryPart("wordone"), new ExactWordQueryPart("wordtwo")));

            VerifyResult(result, expectedQuery);
        }

        [Fact]
        public void ParsingBracketedOrExpressions_ShouldReturnBracketedQueryPartContainer()
        {
            var result = this.Parse("(wordone | wordtwo)");
            var expectedQuery = new BracketedQueryPart(
                    new OrQueryOperator(new ExactWordQueryPart("wordone"), new ExactWordQueryPart("wordtwo")));

            VerifyResult(result, expectedQuery);
        }

        [Fact]
        public void ParsingTwoWordsWithNearOperator_ShouldComposeWithNearOperatorWithToleranceOf5ByDefault()
        {
            var result = this.Parse("wordone ~ wordtwo");
            var expectedQuery = new NearQueryOperator(new ExactWordQueryPart("wordone"), new ExactWordQueryPart("wordtwo"), 5);
            VerifyResult(result, expectedQuery);
        }

        [Theory]
        [InlineData("wordone ~4 wordtwo", 4)]
        [InlineData("wordone ~12 wordtwo", 12)]
        [InlineData("wordone ~1234567890 wordtwo", 1234567890)]
        public void ParsingTwoWordsWithNearOperator_ShouldComposeWithNearOperatorWithSpecifiedTolerance(string query, int expectedTolerance)
        {
            var result = this.Parse(query);
            var expectedQuery = new NearQueryOperator(new ExactWordQueryPart("wordone"), new ExactWordQueryPart("wordtwo"), expectedTolerance);
            VerifyResult(result, expectedQuery);
        }

        [Fact]
        public void ParsingTwoWordsWithPrecedingNearOperator_ShouldComposeWithPrecedingNearOperatorWithToleranceOf5ByDefault()
        {
            var result = this.Parse("wordone ~> wordtwo");
            var expectedQuery = new PrecedingNearQueryOperator(new ExactWordQueryPart("wordone"), new ExactWordQueryPart("wordtwo"), 5);
            VerifyResult(result, expectedQuery);
        }

        [Fact]
        public void ParsingWordsInQuotes_ShouldResultInAdjacentWordsQueryOperator()
        {
            var result = this.Parse("\"search words startswith* too\"");
            var expectedQuery = new AdjacentWordsQueryOperator(
                new IQueryPart[]
                {
                    new ExactWordQueryPart("search"),
                    new ExactWordQueryPart("words"),
                    new WildcardQueryPart(WildcardQueryFragment.CreateText("startswith"), WildcardQueryFragment.MultiCharacter),
                    new ExactWordQueryPart("too")
                });

            VerifyResult(result, expectedQuery);
        }

        [Fact]
        public void ParsingOperatorsInQuotes_ShouldTreatAsText()
        {
            var result = this.Parse("\"test & hello\"");
            var expectedQuery = new AdjacentWordsQueryOperator(
                new IQueryPart[]
                {
                    new ExactWordQueryPart("test"),
                    new ExactWordQueryPart("&"),
                    new ExactWordQueryPart("hello")
                });

            VerifyResult(result, expectedQuery);
        }

        [Theory]
        [InlineData("wordone ~4> wordtwo", 4)]
        [InlineData("wordone ~12> wordtwo", 12)]
        [InlineData("wordone ~1234567890> wordtwo", 1234567890)]
        public void ParsingTwoWordsWithPrecedingNearOperator_ShouldComposeWithPrecedingNearOperatorWithSpecifiedTolerance(string query, int expectedTolerance)
        {
            var result = this.Parse(query);
            var expectedQuery = new PrecedingNearQueryOperator(new ExactWordQueryPart("wordone"), new ExactWordQueryPart("wordtwo"), expectedTolerance);
            VerifyResult(result, expectedQuery);
        }

        [Theory]
        [InlineData("word*")]
        [InlineData(" word*")]
        [InlineData(" word*  ")]
        public void ParsingWordWithWildcard_ShouldReturnStartsWithWordQueryPart(string test)
        {
            var result = this.Parse(test);
            var expectedQuery = new WildcardQueryPart(WildcardQueryFragment.CreateText("word"), WildcardQueryFragment.MultiCharacter);
            VerifyResult(result, expectedQuery);
        }

        [Fact]
        public void ParsingEmptyString_ShouldReturnEmptyQueryPart()
        {
            var result = this.Parse("");
            result.Root.Should().Be(EmptyQueryPart.Instance);
        }

        [Fact]
        public void ParsingBinaryOperatorWithEmptyBracketedPartOnRight_ShouldThrowInvalidQueryException()
        {
            Assert.Throws<QueryParserException>(() => this.Parse("test & ()"));
        }

        [Fact]
        public void ParsingFieldFilterWithSingleWord_ShouldReturnFieldFilterOperatorWithWordAsChild()
        {
            var result = this.Parse("testfield=test");
            var expectedQuery = new FieldFilterQueryOperator("testfield", TestFieldId, new ExactWordQueryPart("test"));
            VerifyResult(result, expectedQuery);
        }

        [Fact]
        public void ParsingFieldFilterWithBracketedStatement_ShouldReturnFieldFilterOperatorWithStatementAsChild()
        {
            var result = this.Parse("testfield=(test | foo) & otherfield=(foo & bar)");
            var expectedQuery =
                new AndQueryOperator(
                    new FieldFilterQueryOperator(
                        "testfield",
                        TestFieldId,
                        new BracketedQueryPart(
                            new OrQueryOperator(new ExactWordQueryPart("test"), new ExactWordQueryPart("foo")))),
                    new FieldFilterQueryOperator(
                        "otherfield",
                        OtherFieldId,
                        new BracketedQueryPart(
                            new AndQueryOperator(new ExactWordQueryPart("foo"), new ExactWordQueryPart("bar")))));

            VerifyResult(result, expectedQuery);
        }

        private static void VerifyResult(IQuery result, IQueryPart expectedQuery)
        {
            result.Root.ToString().Should().Be(expectedQuery.ToString());
        }

        private IQuery Parse(string text)
        {
            var parser = new QueryParser();
            return parser.Parse(this.fieldLookupMock.Object, text, new FakeTokenizer());
        }
    }
}

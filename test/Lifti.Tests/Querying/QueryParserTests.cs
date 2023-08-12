using FluentAssertions;
using Lifti.Querying;
using Lifti.Querying.QueryParts;
using Lifti.Tests.Fakes;
using Lifti.Tokenization;
using Lifti.Tokenization.TextExtraction;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Lifti.Tests.Querying
{
    public class QueryParserTests
    {
        private const byte TestFieldId = 9;
        private const byte OtherFieldId = 11;
        private readonly FakeIndexedFieldLookup fieldLookup;
        private readonly FakeIndexTokenizer field1Tokenizer;
        private readonly FakeIndexTokenizer field2Tokenizer;

        public QueryParserTests()
        {
            var testFieldId = TestFieldId;
            var otherFieldId = OtherFieldId;

            var thesaurus = Thesaurus.Empty;
            this.field1Tokenizer = new FakeIndexTokenizer();
            this.field2Tokenizer = new FakeIndexTokenizer();
            var textExtractor = new PlainTextExtractor();

            var nullFieldReader = (TestObject x, CancellationToken token) => new ValueTask<IEnumerable<string>>(Array.Empty<string>());

            this.fieldLookup = new FakeIndexedFieldLookup(
                ("testfield", IndexedFieldDetails<TestObject>.Static(testFieldId, "testfield", nullFieldReader, textExtractor, this.field1Tokenizer, thesaurus)),
                ("otherfield", IndexedFieldDetails<TestObject>.Static(otherFieldId, "otherfield", nullFieldReader, textExtractor, this.field2Tokenizer, thesaurus))
            );
        }

        [Fact]
        public void ParsingTwoWordsWithNoOperator_WithAndOperatorAsDefault_ShouldComposeWithAndOperator()
        {
            var result = this.Parse("wordone wordtwo");
            var expectedQuery = new AndQueryOperator(new ExactWordQueryPart("wordone"), new ExactWordQueryPart("wordtwo"));
            VerifyResult(result, expectedQuery);
        }

        [Fact]
        public void ParsingTwoWordsWithNoOperator_WithOrOperatorAsDefault_ShouldComposeWithOrOperator()
        {
            var result = this.Parse("wordone wordtwo", defaultJoinOperator: QueryTermJoinOperatorKind.Or);
            var expectedQuery = new OrQueryOperator(new ExactWordQueryPart("wordone"), new ExactWordQueryPart("wordtwo"));
            VerifyResult(result, expectedQuery);
        }

        [Fact]
        public void ParsingTwoWordsWithNoOperator_ShouldComposeWithAndOperator()
        {
            var result = this.Parse("wordone wordtwo");
            var expectedQuery = new AndQueryOperator(new ExactWordQueryPart("wordone"), new ExactWordQueryPart("wordtwo"));
            VerifyResult(result, expectedQuery);
        }

        [Fact]
        public void AssumingFuzzySearch_ShouldTreatWordsAsFuzzySearch()
        {
            var result = this.Parse("wordone", assumeFuzzy: true);
            var expectedQuery = new FuzzyMatchQueryPart("wordone");
            VerifyResult(result, expectedQuery);
        }

        [Fact]
        public void ParsingFuzzyWordWithNoParameters_ShouldProvideParametersFromProvidedFunctions()
        {
            var result = this.Parse("wordone", assumeFuzzy: true, i => (ushort)(i * 10), i => (ushort)(i * 20));
            var expectedQuery = new FuzzyMatchQueryPart("wordone", 70, 140);
            VerifyResult(result, expectedQuery);
        }

        [Fact]
        public void ParsingFuzzyWordWithExplicitParameters_ShouldProvideOverrideDefaults()
        {
            var result = this.Parse("?9,5?wordone", assumeFuzzy: true, i => (ushort)(i * 10), i => (ushort)(i * 20));
            var expectedQuery = new FuzzyMatchQueryPart("wordone", 9, 5);
            VerifyResult(result, expectedQuery);
        }

        [Fact]
        public void ParsingTwoFuzzyWordsWithNoOperator_ShouldComposeWithAndOperator()
        {
            var result = this.Parse("?wordone ?wordtwo");
            var expectedQuery = new AndQueryOperator(new FuzzyMatchQueryPart("wordone"), new FuzzyMatchQueryPart("wordtwo"));
            VerifyResult(result, expectedQuery);
        }

        [Fact]
        public void ParsingMixOfWordMatchesWithNoOperator_ShouldComposeWithAndOperators()
        {
            var result = this.Parse("?wordone wordtwo wor* ?3,?wordthree");
            var expectedQuery =
                new AndQueryOperator(
                    new AndQueryOperator(
                        new AndQueryOperator(
                            new FuzzyMatchQueryPart("wordone"),
                            new ExactWordQueryPart("wordtwo")),
                        new WildcardQueryPart(WildcardQueryFragment.CreateText("wor"), WildcardQueryFragment.MultiCharacter)),
                    new FuzzyMatchQueryPart("wordthree", 3));
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
        public void ParsingPunctuatedWords_ShouldResultInMultipleQueryParts()
        {
            var result = this.Parse("wordone-wordtwo,wordthree");
            var expectedQuery = new AndQueryOperator(
                new AndQueryOperator(new ExactWordQueryPart("wordone"), new ExactWordQueryPart("wordtwo")),
                new ExactWordQueryPart("wordthree"));

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
            var result = this.Parse("\"search words startswith* ?too\"");
            var expectedQuery = new AdjacentWordsQueryOperator(
                new IQueryPart[]
                {
                    new ExactWordQueryPart("search"),
                    new ExactWordQueryPart("words"),
                    new WildcardQueryPart(WildcardQueryFragment.CreateText("startswith"), WildcardQueryFragment.MultiCharacter),
                    new FuzzyMatchQueryPart("too")
                });

            VerifyResult(result, expectedQuery);
        }

        [Fact]
        public void OperatorsInQuotes_ShouldBeTreatedAsSplitChars()
        {
            var result = this.Parse("\"test&hello\"");
            var expectedQuery = new AdjacentWordsQueryOperator(
                new IQueryPart[]
                {
                    new ExactWordQueryPart("test"),
                    new ExactWordQueryPart("hello")
                });

            VerifyResult(result, expectedQuery);
        }

        [Fact]
        public void ParsingQuotes_AssumingFuzzyText_ShouldTreatAsFuzzy()
        {
            var result = this.Parse("\"test hello\"", true);
            var expectedQuery = new AdjacentWordsQueryOperator(
                new IQueryPart[]
                {
                    new FuzzyMatchQueryPart("test"),
                    new FuzzyMatchQueryPart("hello")
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

        private IQuery Parse(
            string text,
            bool assumeFuzzy = false,
            Func<int, ushort>? fuzzySearchMaxEditDistance = null,
            Func<int, ushort>? fuzzySearchMaxSequentialEdits = null,
            QueryTermJoinOperatorKind defaultJoinOperator = QueryTermJoinOperatorKind.And)
        {
            var options = new QueryParserOptions { AssumeFuzzySearchTerms = assumeFuzzy };
            if (fuzzySearchMaxEditDistance != null)
            {
                options.FuzzySearchMaxEditDistance = fuzzySearchMaxEditDistance;
            }
            else
            {
                options.FuzzySearchMaxEditDistance = x => 4;
            }

            if (fuzzySearchMaxSequentialEdits != null)
            {
                options.FuzzySearchMaxSequentialEdits = fuzzySearchMaxSequentialEdits;
            }
            else
            {
                options.FuzzySearchMaxSequentialEdits = x => 1;
            }

            options.DefaultJoiningOperator = defaultJoinOperator;

            var parser = new QueryParser(options);
            return parser.Parse(
                this.fieldLookup,
                text,
                new FakeIndexTokenizerProvider(
                    new FakeIndexTokenizer(),
                    ("testfield", this.field1Tokenizer),
                    ("otherfield", this.field2Tokenizer)));
        }

        private class TestObject
        {
        }
    }
}

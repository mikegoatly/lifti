using FluentAssertions;
using Lifti.Querying;
using Lifti.Querying.QueryParts;
using Lifti.Tokenization;
using System;
using Xunit;

namespace Lifti.Tests.Querying
{
    public class SimpleQueryParserTests
    {
        [Fact]
        public void PunctuationInTerms_ShouldBeStripped()
        {
            var result = Parse("wordone,wordtwo?! (wordthree)");
            var expectedQuery = AndQueryOperator.CombineAll(new[] { new ExactWordQueryPart("WORDONE"), new ExactWordQueryPart("WORDTWO"), new ExactWordQueryPart("WORDTHREE") });
            VerifyResult(result, expectedQuery);
        }

        [Fact]
        public void ParsingTwoWordsWithNoOperator_WithAndOperatorAsDefault_ShouldComposeWithAndOperator()
        {
            var result = Parse("wordone wordtwo");
            var expectedQuery = new AndQueryOperator(new ExactWordQueryPart("WORDONE"), new ExactWordQueryPart("WORDTWO"));
            VerifyResult(result, expectedQuery);
        }

        [Fact]
        public void ParsingTwoWordsWithNoOperator_WithOrOperatorAsDefault_ShouldComposeWithOrOperator()
        {
            var result = Parse("wordone wordtwo", defaultJoinOperator: QueryTermJoinOperatorKind.Or);
            var expectedQuery = new OrQueryOperator(new ExactWordQueryPart("WORDONE"), new ExactWordQueryPart("WORDTWO"));
            VerifyResult(result, expectedQuery);
        }

        [Fact]
        public void AssumingFuzzySearch_ShouldTreatWordsAsFuzzySearch()
        {
            var result = Parse("wordone", assumeFuzzy: true);
            var expectedQuery = new FuzzyMatchQueryPart("WORDONE");
            VerifyResult(result, expectedQuery);
        }

        [Fact]
        public void AssumingFuzzySearch_WithFuzzyDefaults_ShouldTreatWordsAsFuzzySearch()
        {
            var result = Parse("wordone", assumeFuzzy: true, x => (ushort)(x * 10), x => (ushort)(x * 20));
            var expectedQuery = new FuzzyMatchQueryPart("WORDONE", 70, 140);
            VerifyResult(result, expectedQuery);
        }

        private static void VerifyResult(IQuery result, IQueryPart expectedQuery)
        {
            result.Root.ToString().Should().Be(expectedQuery.ToString());
        }

        private static IQuery Parse(
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

            var parser = new SimpleQueryParser(options);
            return parser.Parse(null!, text, new FakeIndexTokenizerProvider(new IndexTokenizer(new TokenizationOptions())));
        }
    }
}

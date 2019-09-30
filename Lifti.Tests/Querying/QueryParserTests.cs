using FluentAssertions;
using Lifti.Querying;
using Lifti.Querying.QueryParts;
using Lifti.Tokenization;
using Moq;
using Xunit;

namespace Lifti.Tests.Querying
{
    public class QueryParserTests
    {
        private readonly Mock<ITokenizer> tokenizerMock;

        public QueryParserTests()
        {
            this.tokenizerMock = new Mock<ITokenizer>();
            this.tokenizerMock.Setup(m => m.Process(It.IsAny<string>())).Returns((string data) => new[] { new Token(data, new WordLocation(0, 0, data.Length)) });
        }

        [Fact]
        public void ParsingTwoWordsWithNoOperator_ShouldComposeWithAndOperator()
        {
            var result = this.Parse("wordone wordtwo");
            var expectedQuery = new Query(new AndQueryOperator(new ExactWordQueryPart("wordone"), new ExactWordQueryPart("wordtwo")));
            result.Should().BeEquivalentTo(expectedQuery);
        }

        [Fact]
        public void ParsingTwoWordsWithAndOperator_ShouldComposeWithAndOperator()
        {
            var result = this.Parse("wordone & wordtwo");
            var expectedQuery = new Query(new AndQueryOperator(new ExactWordQueryPart("wordone"), new ExactWordQueryPart("wordtwo")));
            result.Should().BeEquivalentTo(expectedQuery);
        }

        [Fact]
        public void ParsingTwoWordsWithPrecedingOperator_ShouldComposeWithPrecedingOperator()
        {
            var result = this.Parse("wordone > wordtwo");
            var expectedQuery = new Query(new PrecedingQueryOperator(new ExactWordQueryPart("wordone"), new ExactWordQueryPart("wordtwo")));
            result.Should().BeEquivalentTo(expectedQuery);
        }

        [Fact]
        public void ParsingSingleExactWord_ShouldReturnExactWordQueryPart()
        {
            var result = this.Parse("wordone");
            var expectedQuery = new Query(new ExactWordQueryPart("wordone"));
            result.Should().BeEquivalentTo(expectedQuery);
        }

        [Fact]
        public void ParsingTwoWordsWithNearOperator_ShouldComposeWithNearOperatorWithToleranceOf5ByDefault()
        {
            var result = this.Parse("wordone ~ wordtwo");
            var expectedQuery = new Query(new NearQueryOperator(new ExactWordQueryPart("wordone"), new ExactWordQueryPart("wordtwo"), 5));
            result.Should().BeEquivalentTo(expectedQuery);
        }

        [Theory]
        [InlineData("wordone ~4 wordtwo", 4)]
        [InlineData("wordone ~12 wordtwo", 12)]
        [InlineData("wordone ~1234567890 wordtwo", 1234567890)]
        public void ParsingTwoWordsWithNearOperator_ShouldComposeWithNearOperatorWithSpecifiedTolerance(string query, int expectedTolerance)
        {
            var result = this.Parse(query);
            var expectedQuery = new Query(new NearQueryOperator(new ExactWordQueryPart("wordone"), new ExactWordQueryPart("wordtwo"), expectedTolerance));
            result.Should().BeEquivalentTo(expectedQuery);
        }

        [Fact]
        public void ParsingTwoWordsWithPrecedingNearOperator_ShouldComposeWithPrecedingNearOperatorWithToleranceOf5ByDefault()
        {
            var result = this.Parse("wordone ~> wordtwo");
            var expectedQuery = new Query(new PrecedingNearQueryOperator(new ExactWordQueryPart("wordone"), new ExactWordQueryPart("wordtwo"), 5));
            result.Should().BeEquivalentTo(expectedQuery);
        }

        [Theory]
        [InlineData("wordone ~4> wordtwo", 4)]
        [InlineData("wordone ~12> wordtwo", 12)]
        [InlineData("wordone ~1234567890> wordtwo", 1234567890)]
        public void ParsingTwoWordsWithPrecedingNearOperator_ShouldComposeWithPrecedingNearOperatorWithSpecifiedTolerance(string query, int expectedTolerance)
        {
            var result = this.Parse(query);
            var expectedQuery = new Query(new PrecedingNearQueryOperator(new ExactWordQueryPart("wordone"), new ExactWordQueryPart("wordtwo"), expectedTolerance));
            result.Should().BeEquivalentTo(expectedQuery);
        }

        [Theory]
        [InlineData("word*")]
        [InlineData(" word*")]
        [InlineData(" word*  ")]
        public void ParsingWordWithWildcard_ShouldReturnStartsWithWordQueryPart(string test)
        {
            var result = this.Parse(test);
            var expectedQuery = new Query(new StartsWithWordQueryPart("word"));
            result.Should().BeEquivalentTo(expectedQuery);
        }

        [Fact]
        public void ParsingEmptyString_ShouldReturnNullQueryRoot()
        {
            var result = this.Parse("");
            var expectedQuery = new Query(null);
            result.Should().BeEquivalentTo(expectedQuery);
        }

        private IQuery Parse(string text)
        {
            var parser = new QueryParser();
            return parser.Parse(text, new FakeTokenizer());
        }
    }
}

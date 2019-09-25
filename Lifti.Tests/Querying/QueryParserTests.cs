using FluentAssertions;
using Lifti.Querying;
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
            this.tokenizerMock.Setup(m => m.Process(It.IsAny<string>())).Returns((string data) => new[] { new Token(data, new Range(0, data.Length)) });
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
        public void ParsingTwoWordsWithOrOperator_ShouldComposeWithOrOperator()
        {
            var result = this.Parse("wordone | wordtwo");
            var expectedQuery = new Query(new OrQueryOperator(new ExactWordQueryPart("wordone"), new ExactWordQueryPart("wordtwo")));
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
        public void ParsingEmptyString_ShouldReturnNullQueryRoot()
        {
            var result = this.Parse("");
            var expectedQuery = new Query(null);
            result.Should().BeEquivalentTo(expectedQuery);
        }

        private IQuery Parse(string text)
        {
            var parser = new QueryParser();
            return parser.Parse(text, this.tokenizerMock.Object);
        }
    }
}

using FluentAssertions;
using Lifti.Querying;
using Xunit;

namespace Lifti.Tests.Querying
{
    public class IndexNavigatorTests
    {
        private readonly FullTextIndex<string> index;
        private readonly IndexNavigator sut;

        public IndexNavigatorTests()
        {
            this.index = new FullTextIndex<string>();
            this.index.Index("A", "Triumphant elephant strode elegantly with indifference to shouting subjects, giving withering looks to individuals");
            this.sut = new IndexNavigator(this.index.Root);
        }

        [Theory]
        [InlineData("IND")]
        [InlineData("INDI")]
        [InlineData("INDIF")]
        [InlineData("INDIFFERENC")]
        [InlineData("I")]
        public void GettingExactMatches_WithNoExactMatch_ShouldReturnEmptyResults(string test)
        {
            this.sut.Process(test).Should().BeTrue();
            var results = this.sut.GetExactMatches();
            results.Should().NotBeNull();
            results.Matches.Should().BeEmpty();
        }

        [Theory]
        [InlineData("INDIFZZ")]
        [InlineData("Z")]
        public void GettingExactMatches_WithNonMatchingTextProcessed_ShouldReturnEmptyResults(string test)
        {
            this.sut.Process(test).Should().BeFalse();
            var results = this.sut.GetExactMatches();
            results.Should().NotBeNull();
            results.Matches.Should().BeEmpty();
        }

        [Fact]
        public void GettingExactMatches_WithMatchingTextProcessed_ShouldReturnResults()
        {
            this.sut.Process("INDIFFERENCE").Should().BeTrue();
            var results = this.sut.GetExactMatches();
            results.Should().NotBeNull();
            results.Matches.Should().BeEquivalentTo(
                new[]
                {
                    (0,  new[] { new IndexedWordLocation(0, new Range(42, 12)) })
                });
        }

        [Theory]
        [InlineData("IND")]
        [InlineData("INDI")]
        [InlineData("INDIF")]
        [InlineData("INDIFFERENC")]
        [InlineData("I")]
        public void GettingExactAndChildMatches_WithNoExactMatch_ShouldReturnNonEmptyResults(string test)
        {
            this.sut.Process(test).Should().BeTrue();
            var results = this.sut.GetExactAndChildMatches();
            results.Should().NotBeNull();
            results.Matches.Should().NotBeEmpty();
        }

        [Theory]
        [InlineData("INDIFZZ")]
        [InlineData("Z")]
        public void GettingExactAndChildMatches_WithNonMatchingTextProcessed_ShouldReturnEmptyResults(string test)
        {
            this.sut.Process(test).Should().BeFalse();
            var results = this.sut.GetExactAndChildMatches();
            results.Should().NotBeNull();
            results.Matches.Should().BeEmpty();
        }

        [Fact]
        public void NavigatingLetterByLetter_ShouldReturnTrueUntilNoMatch()
        {
            this.sut.Process('T').Should().BeTrue();
            this.sut.Process('R').Should().BeTrue();
            this.sut.Process('I').Should().BeTrue();
            this.sut.Process('U').Should().BeTrue();
            this.sut.Process('M').Should().BeTrue();
            this.sut.Process('P').Should().BeTrue();
            this.sut.Process('Z').Should().BeFalse();
            this.sut.Process('Z').Should().BeFalse();
        }

        [Theory]
        [InlineData("TRIUMP")]
        [InlineData("SHOUT")]
        [InlineData("WITH")]
        [InlineData("INDIVIDUALS")]
        public void NavigatingByString_ShouldReturnTrueIfEntireStringMatches(string test)
        {
            this.sut.Process(test).Should().BeTrue();
        }

        [Theory]
        [InlineData("TRIUMPZ")]
        [InlineData("SHOUTED")]
        [InlineData("WITHOUT")]
        [InlineData("ELF")]
        public void NavigatingByString_ShouldReturnFalseIfEntireStringDoesntMatch(string test)
        {
            this.sut.Process(test).Should().BeFalse();
        }
    }
}

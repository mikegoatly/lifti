using FluentAssertions;
using Lifti.Querying;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Lifti.Tests.Querying
{
    public class IndexNavigatorTests : QueryTestBase, IAsyncLifetime
    {
        private FullTextIndex<string> index;
        private IIndexNavigator sut;

        public async Task InitializeAsync()
        {
            this.index = new FullTextIndexBuilder<string>()
                .WithItemTokenization<(string, string, string)>(
                    o => o.WithKey(i => i.Item1)
                        .WithField("Field1", i => i.Item2)
                        .WithField("Field2", i => i.Item3))
                .Build();

            await this.index.AddAsync("A", "Triumphant elephant strode elegantly with indifference to shouting subjects, giving withering looks to individuals");
            this.sut = this.index.Snapshot.CreateNavigator();
        }

        public Task DisposeAsync()
        {
            return Task.CompletedTask;
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
                    QueryWordMatch(0, new FieldMatch(0, new SingleWordLocationMatch(new WordLocation(5, 42, 12))))
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

        [Fact]
        public void EnumeratingIndexedWords_WhenAtStartOfNode_ShouldReturnAppropriateWords()
        {
            this.sut.Process("INDI");
            this.sut.EnumerateIndexedWords().Should().BeEquivalentTo(
                "INDIFFERENCE",
                "INDIVIDUALS");
        }

        [Fact]
        public void EnumeratingIndexedWords_WhenAtMidIntraNodeText_ShouldReturnAppropriateWords()
        {
            this.sut.Process("WITHERI");
            this.sut.EnumerateIndexedWords().Should().BeEquivalentTo("WITHERING");
        }

        [Fact]
        public void EnumeratingIndexedWords_MultipleTimes_ShouldYieldSameResults()
        {
            this.sut.Process("WITHERI");
            this.sut.EnumerateIndexedWords().ToList().Should().BeEquivalentTo(this.sut.EnumerateIndexedWords().ToList());
        }

        [Fact]
        public void EnumeratingIndexedWords_WhenAtRoot_ShouldReturnAllWords()
        {
            this.sut.EnumerateIndexedWords().Should().BeEquivalentTo(
                new[] 
                {
                    "TRIUMPHANT",
                    "ELEPHANT",
                    "STRODE",
                    "ELEGANTLY",
                    "WITH",
                    "INDIFFERENCE",
                    "TO",
                    "SHOUTING",
                    "SUBJECTS",
                    "GIVING",
                    "WITHERING",
                    "LOOKS",
                    "INDIVIDUALS"
                },
                o => o.WithoutStrictOrdering());
        }

        [Fact]
        public void EnumeratingIndexedWords_WhenNoMatch_ShouldReturnEmptyResults()
        {
            this.sut.Process("BLABBBBHHHHHB");
            this.sut.EnumerateIndexedWords().Should().BeEmpty();
        }

        [Fact]
        public async Task GettingExactAndChildMatches_ShouldMergeResultsAcrossFields()
        {
            await this.index.AddAsync(("B", "Zoopla Zoo Zammo", "Zany Zippy Llamas"));
            await this.index.AddAsync(("C", "Zak", "Ziggy Stardust"));

            var navigator = this.index.Snapshot.CreateNavigator();
            navigator.Process("Z").Should().BeTrue();
            var results = navigator.GetExactAndChildMatches();
            results.Should().NotBeNull();
            results.Matches.Should()
                .BeEquivalentTo(
                new QueryWordMatch(
                    1, 
                    new[]
                    {
                        new FieldMatch(1, WordMatch(0, 0, 6), WordMatch(1, 7, 3), WordMatch(2, 11, 5)),
                        new FieldMatch(2, WordMatch(0, 0, 4), WordMatch(1, 5, 5))
                    }),
                new QueryWordMatch(
                    2,
                    new[]
                    {
                        new FieldMatch(1, WordMatch(0, 0, 3)),
                        new FieldMatch(2, WordMatch(0, 0, 5))
                    }));
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

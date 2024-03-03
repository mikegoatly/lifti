using FluentAssertions;
using System.Threading.Tasks;
using Xunit;

namespace Lifti.Tests.Querying
{
    public class FluentQueryBuildingTests : IAsyncLifetime
    {
        private IFullTextIndex<string> index = null!;

        public async Task InitializeAsync()
        {
            this.index = new FullTextIndexBuilder<string>()
                .WithObjectTokenization<TestClass>(
                    o => o.WithKey(x => x.Name)
                        // We use a different tokenizer for the description field, and make it case sensitive. This allows us to
                        // test that the tokenization options are correctly flowed down to the subqueries.
                        .WithField("description", x => x.Description, tokenizationOptions: t => t.CaseInsensitive(false)))
                .Build();
            await this.index.AddAsync("A", "Some test text");
        }

        public Task DisposeAsync()
        {
            return Task.CompletedTask;
        }

        [Fact]
        public void ExecutingQueryReturnsExpectedResults()
        {
            var results = this.index.Query().ExactMatch("some").Execute();

            results.Should().HaveCount(1);

            results = this.index.Query().WildcardMatch("so%%").Execute();

            results.Should().HaveCount(1);

            results = this.index.Query().FuzzyMatch("som").Execute();

            results.Should().HaveCount(1);

            results = this.index.Query().Adjacent(a => a.ExactMatch("some").ExactMatch("test")).Execute();

            results.Should().HaveCount(1);
        }

        [Fact]
        public void ExactMatch_WithNoScoreBoost()
        {
            var query = this.index.Query().ExactMatch("some").Build();

            query.ToString().Should().Be("SOME");
        }

        [Fact]
        public void ExactMatch_WithScoreBoost()
        {
            var query = this.index.Query().ExactMatch("some", 2D).Build();

            query.ToString().Should().Be("SOME^2");
        }

        [Fact]
        public void FuzzyMatch_WithNoScoreBoost()
        {
            var query = this.index.Query().FuzzyMatch("some").Build();

            query.ToString().Should().Be("?SOME");
        }

        [Fact]
        public void FuzzyMatch_WithScoreBoost()
        {
            var query = this.index.Query().FuzzyMatch("some", scoreBoost: 2D).Build();

            query.ToString().Should().Be("?SOME^2");
        }

        [Fact]
        public void ParsedWildcardMatch_WithNoScoreBoost()
        {
            var query = this.index.Query().WildcardMatch("%some*").Build();

            query.ToString().Should().Be("%SOME*");
        }

        [Fact]
        public void ParsedWildcardMatch_WithScoreBoost()
        {
            var query = this.index.Query().WildcardMatch("%some*", 2D).Build();

            query.ToString().Should().Be("%SOME*^2");
        }

        [Fact]
        public void ManuallyConstructedWildcardMatch_WithNoScoreBoost()
        {
            var query = this.index.Query().WildcardMatch(m => m.MultipleCharacters().Text("some").SingleCharacter()).Build();

            query.ToString().Should().Be("*SOME%");
        }

        [Fact]
        public void ManuallyConstructedWildcardMatch_WithScoreBoost()
        {
            var query = this.index.Query().WildcardMatch(m => m.MultipleCharacters().Text("some").SingleCharacter(), 2D).Build();

            query.ToString().Should().Be("*SOME%^2");
        }

        [Fact]
        public void FuzzyMatch_WithAllParameters()
        {
            var query = this.index.Query().FuzzyMatch("some", maxEditDistance: 5, maxSequentialEdits: 9, scoreBoost: 2D).Build();

            query.ToString().Should().Be("?5,9?SOME^2");
        }

        [Fact]
        public void CombiningQueriesWithAndOperator()
        {
            var query = this.index.Query()
                .ExactMatch("some")
                .And.ExactMatch("test")
                .Build();

            query.ToString().Should().Be("SOME & TEST");
        }

        [Fact]
        public void CombiningQueriesWithOrOperator()
        {
            var query = this.index.Query()
                .ExactMatch("some")
                .Or.ExactMatch("test")
                .Build();

            query.ToString().Should().Be("SOME | TEST");
        }

        [Fact]
        public void CombiningAndAndOrOperators()
        {
            var query = this.index.Query()
                .ExactMatch("some")
                .And.ExactMatch("test")
                .Or.ExactMatch("text")
                .Build();

            query.ToString().Should().Be("SOME & TEST | TEXT");
        }

        [Fact]
        public void FieldRestrictedQueries()
        {
            var query = this.index.Query()
                .InField("description", f => f.ExactMatch("some"))
                .Build();

            // The description field is case sensitive, so the original text should be unaltered.
            query.ToString().Should().Be("[description]=some");
        }

        [Fact]
        public void BracketedQueries()
        {
            var query = this.index.Query()
                .Bracketed(b => b.ExactMatch("some").And.ExactMatch("test"))
                .Or.Bracketed(b => b.ExactMatch("text").And.ExactMatch("test"))
                .Build();

            query.ToString().Should().Be("(SOME & TEST) | (TEXT & TEST)");
        }

        [Fact]
        public void CompositeFieldRestrictedQueries_ShouldImplictlyBeBracketed()
        {
            var query = this.index.Query()
                .InField("description", f => f.ExactMatch("some").And.ExactMatch("test"))
                .Build();

            // The description field is case sensitive, so the original text should be unaltered.
            query.ToString().Should().Be("[description]=(some & test)");
        }

        [Fact]
        public void ExplicitlyBracketedFieldRestrictedQueries_ShouldNotAlsoBeImplicitlyBracketed()
        {
            var query = this.index.Query()
                .InField("description", f => f.Bracketed(b => b.ExactMatch("some").And.ExactMatch("test")))
                .Build();

            // The description field is case sensitive, so the original text should be unaltered.
            query.ToString().Should().Be("[description]=(some & test)");
        }

        [Fact]
        public void AdjacentWords()
        {
            var query = this.index.Query()
                .Adjacent(a => a
                    .ExactMatch("some", 4D)
                    .ExactMatch("some")
                    .FuzzyMatch("adjacent")
                    .FuzzyMatch("adjacent", 5, 2, 2D)
                    .WildcardMatch("text*", 4D)
                    .WildcardMatch(m => m.MultipleCharacters().Text("Manual").SingleCharacter()))
                .Build();

            query.ToString().Should().Be("\"SOME^4 SOME ?ADJACENT ?5,2?ADJACENT^2 TEXT*^4 *MANUAL%\"");
        }

        [Fact]
        public void CombiningQueriesWithPreceding()
        {
            var query = this.index.Query()
                .ExactMatch("some").Preceding.ExactMatch("where")
                .Build();

            query.ToString().Should().Be("SOME > WHERE");
        }

        [Fact]
        public void CombiningQueriesWithNear_DefaultThreshold()
        {
            var query = this.index.Query()
                .ExactMatch("some").Near().ExactMatch("where")
                .Build();

            query.ToString().Should().Be("SOME ~ WHERE");
        }

        [Fact]
        public void CombiningQueriesWithNear_SpecifiedThreshold()
        {
            var query = this.index.Query()
                .ExactMatch("some").Near(3).ExactMatch("where")
                .Build();

            query.ToString().Should().Be("SOME ~3 WHERE");
        }

        [Fact]
        public void CombiningQueriesWithCloselyPreceding_DefaultThreshold()
        {
            var query = this.index.Query()
                .ExactMatch("some").CloselyPreceding().ExactMatch("where")
                .Build();

            query.ToString().Should().Be("SOME ~> WHERE");
        }

        [Fact]
        public void CombiningQueriesWithCloselyPreceding_SpecifiedThreshold()
        {
            var query = this.index.Query()
                .ExactMatch("some").CloselyPreceding(3).ExactMatch("where")
                .Build();

            query.ToString().Should().Be("SOME ~3> WHERE");
        }

        [Fact]
        public void FieldRestrictedQueries_UseAppropriateTokenizerForTokensInSubqueries()
        {
            var query = this.index.Query()
                .InField("description", 
                    f => f
                        .ExactMatch("Some")
                        .And.Bracketed(b => b.ExactMatch("test"))
                        .And.WildcardMatch("text*"))
                .And.ExactMatch("some")
                .Build();

            // The matches in the description field should be case sensitive, should should be unaltered from the original requested text.
            // The last SOME match is case insensitive, so will be uppercased by the index's default tokenizer.
            query.ToString().Should().Be("[description]=(Some & (test) & text*) & SOME");
        }

        private record TestClass(string Name, string Description);
    }
}

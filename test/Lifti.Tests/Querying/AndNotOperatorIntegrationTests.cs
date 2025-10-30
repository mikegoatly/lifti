using FluentAssertions;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Lifti.Tests.Querying
{
    public class AndNotOperatorIntegrationTests : IAsyncLifetime
    {
        private IFullTextIndex<int> index = null!;

        public async Task InitializeAsync()
        {
            this.index = new FullTextIndexBuilder<int>().Build();

            // Build a test index with various documents
            await this.index.AddAsync(1, "The Eiffel Tower is in Paris");
            await this.index.AddAsync(2, "Paris is a beautiful city");
            await this.index.AddAsync(3, "The Eiffel 65 band is famous");
            await this.index.AddAsync(4, "A bell tower stands in the square");
            await this.index.AddAsync(5, "London has the Tower of London");
            await this.index.AddAsync(6, "The tower at Pisa leans");
            await this.index.AddAsync(7, "Eiffel designed more than just the tower");
        }

        public Task DisposeAsync()
        {
            return Task.CompletedTask;
        }

        [Fact]
        public void SearchingWithAndNotOperator_ShouldExcludeMatchingDocuments()
        {
            var results = this.index.Search("eiffel &! tower");

            // Should match docs 3 (Eiffel 65 band) since it has "eiffel" but not "tower"
            // Should NOT match doc 1 (has both)
            // Should NOT match doc 7 (has both "eiffel" and "tower")
            results.Should().HaveCount(1);
            results.Single().Key.Should().Be(3);
        }

        [Fact]
        public void SearchingWithAndNotOperatorAgainstOrExpression_ShouldExcludeAllMatches()
        {
            var results = this.index.Search("paris &! (eiffel | tower)");

            // Should match doc 2 (Paris without Eiffel or tower)
            // Should NOT match doc 1 (has both Paris and Eiffel and tower)
            results.Should().HaveCount(1);
            results.Single().Key.Should().Be(2);
        }

        [Fact]
        public void AndNotWithAndOperator_ShouldWorkCorrectly()
        {
            var results = this.index.Search("paris & eiffel &! tower");

            // (paris AND eiffel) AND-NOT tower
            // Doc 1 has all three, so should be excluded
            // No other doc has both paris and eiffel
            results.Should().BeEmpty();
        }

        [Fact]
        public void AndNotWithOrOperator_ShouldRespectPrecedence()
        {
            // With &! at same precedence as &, "tower | paris &! eiffel" parses as "tower | (paris &! eiffel)"
            var results = this.index.Search("tower | paris &! eiffel");

            // Left side: tower = docs 1, 4, 5, 6, 7
            // Right side: (paris - eiffel) = doc 2
            // Result: tower | (paris - eiffel) = {1, 4, 5, 6, 7} + { 2 } = { 1, 2, 4, 5, 6, 7 }
            results.Select(r => r.Key).Should().BeEquivalentTo(new[] { 1, 2, 4, 5, 6, 7 });
        }

        [Fact]
        public void AndNotWithBrackets_ShouldOverridePrecedence()
        {
            // "(tower | paris) &! eiffel" should match documents with tower OR paris, but not eiffel
            var results = this.index.Search("(tower | paris) &! eiffel");

            // Left: tower | paris = docs 1, 2, 4, 5, 6
            // Right: eiffel = docs 1, 3, 7
            // Left &! Right = docs 2, 4, 5, 6
            results.Select(r => r.Key).Should().BeEquivalentTo(new[] { 2, 4, 5, 6 });
        }

        [Fact]
        public void AndNotWithNoLeftMatches_ShouldReturnEmpty()
        {
            var results = this.index.Search("nonexistent &! tower");

            results.Should().BeEmpty();
        }

        [Fact]
        public void AndNotWithNoRightMatches_ShouldReturnAllLeftMatches()
        {
            var results = this.index.Search("paris &! nonexistent");

            // All documents with "paris"
            results.Select(r => r.Key).Should().BeEquivalentTo(new[] { 1, 2 });
        }

        [Fact]
        public void AndNotWithNoOverlap_ShouldReturnAllLeftMatches()
        {
            var results = this.index.Search("london &! paris");

            // Doc 5 has "london" but not "paris"
            results.Should().HaveCount(1);
            results.Single().Key.Should().Be(5);
        }

        [Fact]
        public void AndNotWithCompleteOverlap_ShouldReturnEmpty()
        {
            var results = this.index.Search("eiffel & tower &! eiffel");

            // (eiffel AND tower) AND-NOT eiffel
            // No documents can have eiffel AND tower but NOT eiffel
            results.Should().BeEmpty();
        }

        [Fact]
        public void AndNotShouldPreserveScoring()
        {
            var withoutAndNot = this.index.Search("tower").ToList();
            var withAndNot = this.index.Search("tower &! nonexistent").ToList();

            // Scores should be identical since we're not excluding anything
            withAndNot.Should().HaveCount(withoutAndNot.Count);

            foreach (var result in withAndNot)
            {
                var corresponding = withoutAndNot.First(r => r.Key == result.Key);
                result.Score.Should().Be(corresponding.Score);
            }
        }

        [Fact]
        public void ComplexQuery_WithMultipleOperators_ShouldWorkCorrectly()
        {
            // Find architectural terms but exclude specific famous landmarks
            var results = this.index.Search("(tower | architecture) &! (eiffel | london)");

            // Left: tower | architecture = docs 1, 4, 5, 6, 7
            // Right: eiffel | london = docs 1, 3, 5, 7
            // Result: docs 4, 6
            results.Select(r => r.Key).Should().BeEquivalentTo(new[] { 4, 6 });
        }

        [Fact]
        public void AndNotWithWildcards_ShouldWorkCorrectly()
        {
            var results = this.index.Search("paris* &! eif*");

            // paris* matches docs with "paris"
            // eif* matches docs with "eiffel"
            // Result: doc 2 (has "paris" but no "eiffel")
            results.Should().HaveCount(1);
            results.Single().Key.Should().Be(2);
        }

        [Fact]
        public void AndNotWithFuzzySearch_ShouldWorkCorrectly()
        {
            var results = this.index.Search("?tower &! london");

            // ?tower does fuzzy match on "tower"
            // Should match all tower docs except London
            results.Select(r => r.Key).Should().BeEquivalentTo(new[] { 1, 4, 6, 7 });
        }
    }
}

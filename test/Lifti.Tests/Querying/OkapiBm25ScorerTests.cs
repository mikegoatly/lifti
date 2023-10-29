using FluentAssertions;
using Lifti.Querying;
using Lifti.Tests.Fakes;
using System.Collections.Immutable;
using System.Linq;
using Xunit;

namespace Lifti.Tests.Querying
{
    public class OkapiBm25ScorerTests : QueryTestBase
    {
        private const double expectedScore1 = 2.536599214033677D;
        private const double expectedScore2 = 2.3792189708272073D;

        private readonly QueryTokenMatch[] tokenMatches;
        private readonly FakeItemStore<int> itemStore;
        private OkapiBm25Scorer sut;

        public OkapiBm25ScorerTests()
        {
            this.itemStore = new FakeItemStore<int>(
                10,
                new IndexStatistics(ImmutableDictionary<byte, long>.Empty.Add(1, 100), 100), // 100 total tokens in 1 field
                Enumerable.Range(0, 10)
                    .Select(id => (id, new ItemMetadata<int>(id, id, new DocumentStatistics(1, id * 3))))
                    .ToArray()); // Each item will have (id * 3) tokens in it

            this.sut = new OkapiBm25Scorer(1.2D, 0.75D, itemStore, new FakeFieldScoreBoostProvider((2, 10D)));

            this.tokenMatches = new[]
{
                new QueryTokenMatch(1, new[] { FieldMatch(1, 3, 6) }),
                new QueryTokenMatch(3, new[] { FieldMatch(1, 8, 2, 5) })
            };
        }

        [Fact]
        public void VerifyScoreWithoutWeighting()
        {
            var results = this.sut.Score(tokenMatches, 1D);

            results.Should().BeEquivalentTo(
                new[]
                {
                    new ScoredToken(1, new[] { ScoredFieldMatch(expectedScore1, 1, 3, 6) }),
                    new ScoredToken(3, new[] { ScoredFieldMatch(expectedScore2, 1, 8, 2, 5) })
                },
                o => o.Using<double>(ctx => ctx.Subject.Should().BeApproximately(ctx.Expectation, 0.00001D)).When(i => i.RuntimeType == typeof(double)));
        }

        [Fact]
        public void VerifyScoreWithWeighting()
        {
            var results = this.sut.Score(tokenMatches, 0.5D);

            results.Should().BeEquivalentTo(
                new[]
                {
                    new ScoredToken(1, new[] { ScoredFieldMatch(expectedScore1 / 2D, 1, 3, 6) }),
                    new ScoredToken(3, new[] { ScoredFieldMatch(expectedScore2 / 2D, 1, 8, 2, 5) })
                },
                o => o.Using<double>(ctx => ctx.Subject.Should().BeApproximately(ctx.Expectation, 0.00001D)).When(i => i.RuntimeType == typeof(double)));
        }

        [Fact]
        public void VerifyScoreWithFieldWeighting()
        {
            this.sut = new OkapiBm25Scorer(1.2D, 0.75D, this.itemStore, new FakeFieldScoreBoostProvider((1, 10D)));

            var results = this.sut.Score(tokenMatches, 0.5D);

            // Results are calculated with a field score boost of 10, but a multiplier of 0.5D, so the resulting boost is 5
            results.Should().BeEquivalentTo(
                new[]
                {
                    new ScoredToken(1, new[] { ScoredFieldMatch(expectedScore1 * 5, 1, 3, 6) }),
                    new ScoredToken(3, new[] { ScoredFieldMatch(expectedScore2 * 5, 1, 8, 2, 5) })
                },
                o => o.Using<double>(ctx => ctx.Subject.Should().BeApproximately(ctx.Expectation, 0.00001D)).When(i => i.RuntimeType == typeof(double)));
        }
    }
}

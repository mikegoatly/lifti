using FluentAssertions;
using Lifti.Querying;
using Lifti.Tests.Fakes;
using System;
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
        private static readonly FakeItemStore<int> looseTextItemStore = new(
                10,
                new IndexStatistics(ImmutableDictionary<byte, long>.Empty.Add(1, 100), 100), // 100 total tokens in 1 field
                Enumerable.Range(0, 10)
                    .Select(id => (id, ItemMetadata<int>.ForLooseText(id, id, new DocumentStatistics(1, id * 3))))
                    .ToArray(), // Each item will have (id * 3) tokens in it
                Array.Empty<(byte, Func<ItemMetadata, double>)>());

        private static readonly FakeItemStore<int> objectTextItemStore = new(
                10,
                new IndexStatistics(ImmutableDictionary<byte, long>.Empty.Add(1, 100), 100), // 100 total tokens in 1 field
                Enumerable.Range(0, 10)
                    .Select(id => (id, ItemMetadata<int>.ForObject(
                        (byte)((id % 3) + 1), // Each item will be assigned to object type 1, 2, or 3 
                        id,
                        id,
                        new DocumentStatistics(1, id * 3),
                        null,
                        null)))
                    .ToArray(), // Each item will have (id * 3) tokens in it
                new (byte, Func<ItemMetadata, double>)[]
                {
                    (1, (ItemMetadata itemMetadata) => 10D), // 10x score boost for object type 1
                    (2, (ItemMetadata itemMetadata) => 1D), // No field score boost for object type 2
                    (3, (ItemMetadata itemMetadata) => 1D) // No field score boost for object type 3
                });

        public OkapiBm25ScorerTests()
        {
            this.tokenMatches = new[]
            {
                new QueryTokenMatch(1, new[] { FieldMatch(1, 3, 6) }),
                new QueryTokenMatch(3, new[] { FieldMatch(1, 8, 2, 5) })
            };
        }

        [Fact]
        public void VerifyScoreWithoutWeighting()
        {
            var sut = CreateSut(looseTextItemStore);
            var results = sut.Score(this.tokenMatches, 1D);

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
            var sut = CreateSut(looseTextItemStore);
            var results = sut.Score(this.tokenMatches, 0.5D);

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
            var sut = CreateSut(looseTextItemStore, new FakeFieldScoreBoostProvider((1, 10D)));

            var results = sut.Score(this.tokenMatches, 0.5D);

            // Results are calculated with a field score boost of 10, but a multiplier of 0.5D, so the resulting boost is 5
            results.Should().BeEquivalentTo(
                new[]
                {
                    new ScoredToken(1, new[] { ScoredFieldMatch(expectedScore1 * 5, 1, 3, 6) }),
                    new ScoredToken(3, new[] { ScoredFieldMatch(expectedScore2 * 5, 1, 8, 2, 5) })
                },
                o => o.Using<double>(ctx => ctx.Subject.Should().BeApproximately(ctx.Expectation, 0.00001D)).When(i => i.RuntimeType == typeof(double)));
        }

        [Fact]
        public void VerifyScoreWithObjectTypeWeighting()
        {
            var sut = CreateSut(objectTextItemStore, new FakeFieldScoreBoostProvider());

            var results = sut.Score(this.tokenMatches, 1D);

            results.Should().BeEquivalentTo(
                new[]
                {
                    // Document 1 has an object type of 2
                    new ScoredToken(1, new[] { ScoredFieldMatch(expectedScore1, 1, 3, 6) }),
                    // Document 3 has an object type of 1, so gets a 10x boost
                    new ScoredToken(3, new[] { ScoredFieldMatch(expectedScore2 * 10, 1, 8, 2, 5) })
                },
                o => o.Using<double>(ctx => ctx.Subject.Should().BeApproximately(ctx.Expectation, 0.00001D)).When(i => i.RuntimeType == typeof(double)));
        }

        private static OkapiBm25Scorer CreateSut(FakeItemStore<int> itemStore, FakeFieldScoreBoostProvider? fieldScoreBoostProvider = null)
        {
            return new OkapiBm25Scorer(
                1.2D,
                0.75D,
                itemStore,
                fieldScoreBoostProvider ?? new FakeFieldScoreBoostProvider((2, 10D)));
        }
    }
}
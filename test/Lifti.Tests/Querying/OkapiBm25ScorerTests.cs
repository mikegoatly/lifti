using FluentAssertions;
using Lifti.Querying;
using Moq;
using System.Collections.Immutable;
using Xunit;

namespace Lifti.Tests.Querying
{
    public class OkapiBm25ScorerTests : QueryTestBase
    {
        private const double expectedScore1 = 2.536599214033677D;
        private const double expectedScore2 = 2.3792189708272073D;

        private readonly OkapiBm25Scorer sut;
        private readonly QueryTokenMatch[] tokenMatches;

        public OkapiBm25ScorerTests()
        {
            var snapshotMock = new Mock<IItemStore>();
            snapshotMock.SetupGet(s => s.Count).Returns(10); // 10 items in the index
            snapshotMock.SetupGet(s => s.IndexStatistics).Returns(
                new IndexStatistics(ImmutableDictionary<byte, long>.Empty.Add(1, 100), 100)); // 100 total tokens in 1 field

            snapshotMock.Setup(s => s.GetMetadata(It.IsAny<int>())).Returns(
                (int id) => new ItemMetadata<int>(id, id, new DocumentStatistics(1, id * 3))); // Each item will have (id * 3) tokens in it

            this.sut = new OkapiBm25Scorer(1.2D, 0.75D, snapshotMock.Object);

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
    }
}

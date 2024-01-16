using FluentAssertions;
using Lifti.Querying;
using Lifti.Tests.Fakes;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Lifti.Tests.Querying
{
    public class OkapiBm25ScorerTests : QueryTestBase
    {
        private const double expectedScore1 = 2.536599214033677D;
        private const double expectedScore2 = 2.3792189708272073D;

        private static readonly FakeIndexMetadata<int> looseTextIndexMetadata = new(
                10,
                new IndexStatistics(new Dictionary<byte, long>() { { 1, 100 } }, 100), // 100 total tokens in 1 field
                Enumerable.Range(0, 10)
                    .Select(id => (id, DocumentMetadata.ForLooseText(id, id, new DocumentStatistics(1, id * 3))))
                    .ToArray(), // Each item will have (id * 3) tokens in it
                Array.Empty<(byte, Func<DocumentMetadata, double>)>());

        private static readonly FakeIndexMetadata<int> objectTextIndexMetadata = new(
                10,
                new IndexStatistics(new Dictionary<byte, long>() { { 1, 100 } }, 100), // 100 total tokens in 1 field
                Enumerable.Range(0, 10)
                    .Select(id => (id, DocumentMetadata.ForObject(
                        (byte)((id % 3) + 1), // Each item will be assigned to object type 1, 2, or 3 
                        id,
                        id,
                        new DocumentStatistics(1, id * 3),
                        null,
                        null)))
                    .ToArray(), // Each item will have (id * 3) tokens in it
                new (byte, Func<DocumentMetadata, double>)[]
                {
                    (1, (DocumentMetadata metadata) => 10D), // 10x score boost for object type 1
                    (2, (DocumentMetadata metadata) => 1D), // No field score boost for object type 2
                    (3, (DocumentMetadata metadata) => 1D) // No field score boost for object type 3
                });

        [Fact]
        public void VerifyScoreWithoutWeighting()
        {
            var sut = CreateSut(looseTextIndexMetadata);
            VerifyScore(sut, 2, 1, 1, TokenLocations(3, 6), 1D, expectedScore1);
            VerifyScore(sut, 2, 3, 1, TokenLocations(8, 2, 5), 1D, expectedScore2);
        }

        [Fact]
        public void VerifyScoreWithWeighting()
        {
            var sut = CreateSut(looseTextIndexMetadata);
            VerifyScore(sut, 2, 1, 1, TokenLocations(3, 6), 0.5D, expectedScore1 / 2);
            VerifyScore(sut, 2, 3, 1, TokenLocations(8, 2, 5), 0.5D, expectedScore2 / 2);
        }

        [Fact]
        public void VerifyScoreWithFieldWeighting()
        {
            var sut = CreateSut(looseTextIndexMetadata, new FakeFieldScoreBoostProvider((1, 10D)));

            // Results are calculated with a field score boost of 10, but a multiplier of 0.5D, so the resulting boost is 5
            VerifyScore(sut, 2, 1, 1, TokenLocations(3, 6), 0.5D, expectedScore1 * 5);
            VerifyScore(sut, 2, 3, 1, TokenLocations(8, 2, 5), 0.5D, expectedScore2 * 5);
        }

        [Fact]
        public void VerifyScoreWithObjectTypeWeighting()
        {
            var sut = CreateSut(objectTextIndexMetadata, new FakeFieldScoreBoostProvider());

            // Document 1 has an object type of 2
            VerifyScore(sut, 2, 1, 1, TokenLocations(3, 6), 1D, expectedScore1);
            // Document 3 has an object type of 1, so gets a 10x boost
            VerifyScore(sut, 2, 3, 1, TokenLocations(8, 2, 5), 1D, expectedScore2 * 10);
        }

        private static void VerifyScore(
            OkapiBm25Scorer sut,
            int totalMatchedDocuments,
            int documentId,
            byte fieldId,
            IReadOnlyList<TokenLocation> tokenLocations,
            double weighting,
            double expectedScore)
        {
            var result = sut.CalculateScore(
                totalMatchedDocuments,
                documentId,
                fieldId,
                tokenLocations,
                weighting);

            result.Should().BeApproximately(expectedScore, 0.00001D);
        }

        private static OkapiBm25Scorer CreateSut(FakeIndexMetadata<int> itemStore, FakeFieldScoreBoostProvider? fieldScoreBoostProvider = null)
        {
            return new OkapiBm25Scorer(
                1.2D,
                0.75D,
                itemStore,
                fieldScoreBoostProvider ?? new FakeFieldScoreBoostProvider((2, 10D)));
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;

namespace Lifti.Tests.Fakes
{
    internal class FakeIndexMetadata<TKey> : IIndexMetadata
    {
        private readonly Dictionary<int, DocumentMetadata<TKey>> documentMetadata;
        private readonly Dictionary<byte, Func<DocumentMetadata, double>> objectTypeMetadata;

        public FakeIndexMetadata(
            int count,
            IndexStatistics statistics,
            (int documentId, DocumentMetadata<TKey> statistics)[] documentMetadata,
            (byte objectTypeId, Func<DocumentMetadata, double> scoreProvider)[] objectTypeMetadata)
        {
            this.DocumentCount = count;
            this.IndexStatistics = statistics;
            this.documentMetadata = documentMetadata.ToDictionary(i => i.documentId, i => i.statistics);
            this.objectTypeMetadata = objectTypeMetadata.ToDictionary(i => i.objectTypeId, i => i.scoreProvider);
        }

        public int DocumentCount { get; private set; }

        public IndexStatistics IndexStatistics { get; private set; }

        public int Count => this.DocumentCount;

        public DocumentMetadata GetMetadata(int documentId)
        {
            return this.documentMetadata[documentId];
        }

        public ScoreBoostMetadata GetObjectTypeScoreBoostMetadata(byte objectTypeId)
        {
            return new FakeScoreBoostMetadata(this.objectTypeMetadata[objectTypeId]);
        }

        private class FakeScoreBoostMetadata : ScoreBoostMetadata
        {
            private readonly Func<DocumentMetadata, double> scoreBoostCalculator;

            public FakeScoreBoostMetadata(Func<DocumentMetadata, double> func)
                : base(null!)
            {
                this.scoreBoostCalculator = func;
            }

            public override double CalculateScoreBoost(DocumentMetadata documentMetadata)
            {
                return this.scoreBoostCalculator(documentMetadata);
            }
        }
    }
}

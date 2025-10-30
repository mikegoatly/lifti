using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Lifti.Querying
{
    /// <summary>
    /// An implementation of the Okapi BM-25 scorer.
    /// </summary>
    internal class OkapiBm25Scorer : IScorer
    {
        private readonly Dictionary<byte, double> averageTokenCountByField;
        private readonly ConcurrentDictionary<(int documentId, byte fieldId), (double scoreBoost, double tokensInDocumentWeighting)> documentFieldCache = new();
        private readonly ConcurrentDictionary<int, double> idfCache = new();
        private readonly double documentCount;
        private readonly double k1;
        private readonly double k1PlusOne;
        private readonly double b;
        private readonly IIndexMetadata indexMetadata;
        private readonly IFieldScoreBoostProvider fieldScoreBoosts;

        /// <summary>
        /// Constructs a new instance of the <see cref="OkapiBm25Scorer"/>.
        /// </summary>
        /// <param name="k1">The "k1" parameter for the scorer.</param>
        /// <param name="b">The "b" parameter for the scorer.</param>
        /// <param name="indexMetadata">
        /// The <see cref="IIndexMetadata"/> of the index snapshot being queried.
        /// </param>
        /// <param name="fieldScoreBoosts">
        /// The <see cref="IFieldScoreBoostProvider"/> to use to get the score boost for a field.
        /// </param>
        internal OkapiBm25Scorer(double k1, double b, IIndexMetadata indexMetadata, IFieldScoreBoostProvider fieldScoreBoosts)
        {
            ArgumentNullException.ThrowIfNull(indexMetadata);

            var documentCount = (double)indexMetadata.DocumentCount;
            this.averageTokenCountByField = indexMetadata.IndexStatistics.TokenCountByField.ToDictionary(k => k.Key, k => k.Value / documentCount);
            this.documentCount = documentCount;
            this.k1 = k1;
            this.k1PlusOne = k1 + 1D;
            this.b = b;
            this.indexMetadata = indexMetadata;
            this.fieldScoreBoosts = fieldScoreBoosts;
        }

        public double CalculateScore(int totalMatchedDocuments, int documentId, byte fieldId, IReadOnlyList<TokenLocation> tokenLocations, double weighting)
        {
            var idf = this.CalculateInverseDocumentFrequency(totalMatchedDocuments);

            double scoreBoost;
            double tokensInDocumentWeighting;
            if (this.documentFieldCache.TryGetValue((documentId, fieldId), out var cacheEntry))
            {
                (scoreBoost, tokensInDocumentWeighting) = cacheEntry;
            }
            else
            {
                var documentMetadata = this.indexMetadata.GetDocumentMetadata(documentId);
                var fieldStats = documentMetadata.DocumentStatistics.StatisticsByField[fieldId];
                var tokensInDocument = fieldStats.TokenCount;
                tokensInDocumentWeighting = tokensInDocument / this.averageTokenCountByField[fieldId];

                // We can cache the score boost for the field and object type (if applicable) because it won't
                // change for the lifetime of the associated index snapshot.
                scoreBoost = this.fieldScoreBoosts.GetScoreBoost(fieldId);
                if (documentMetadata.ObjectTypeId is { } objectTypeId)
                {
                    var objectScoreBoostMetadata = this.indexMetadata.GetObjectTypeScoreBoostMetadata(objectTypeId);
                    scoreBoost *= objectScoreBoostMetadata.CalculateScoreBoost(documentMetadata);
                }

                this.documentFieldCache.TryAdd((documentId, fieldId), (scoreBoost, tokensInDocumentWeighting));
            }

            var frequencyInDocument = tokenLocations.Count;
            var numerator = frequencyInDocument * this.k1PlusOne;
            var denominator = frequencyInDocument + (this.k1 * (1 - this.b + (this.b * tokensInDocumentWeighting)));

            var fieldScore = idf * (numerator / denominator);

            return fieldScore * weighting * scoreBoost;
        }

        private double CalculateInverseDocumentFrequency(int matchedDocumentCount)
        {
            if (!this.idfCache.TryGetValue(matchedDocumentCount, out var idf))
            {
                idf = (this.documentCount - matchedDocumentCount + 0.5D)
                    / (matchedDocumentCount + 0.5D);

                idf = Math.Log(1D + idf);

                this.idfCache.TryAdd(matchedDocumentCount, idf);
            }

            return idf;
        }
    }
}

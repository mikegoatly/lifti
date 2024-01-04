using System;
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
            if (indexMetadata is null)
            {
                throw new ArgumentNullException(nameof(indexMetadata));
            }

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
            // TODO LRU cache idf?
            var idf = this.CalculateInverseDocumentFrequency(totalMatchedDocuments);

            var documentMetadata = this.indexMetadata.GetDocumentMetadata(documentId);

            // TODO LRU cache objectScoreBoost by documentId?

            // TODO LRU cache tokensInDocumentWeighting by documentId and fieldId?
            var documentTokenCounts = documentMetadata.DocumentStatistics.TokenCountByField;
            var tokensInDocument = documentTokenCounts[fieldId];
            var tokensInDocumentWeighting = tokensInDocument / this.averageTokenCountByField[fieldId];

            var frequencyInDocument = tokenLocations.Count;
            var numerator = frequencyInDocument * this.k1PlusOne;
            var denominator = frequencyInDocument + (this.k1 * (1 - this.b + (this.b * tokensInDocumentWeighting)));

            var fieldScore = idf * (numerator / denominator);
            var fieldScoreBoost = this.fieldScoreBoosts.GetScoreBoost(fieldId);

            var weightedScore = fieldScore * weighting * fieldScoreBoost;

            if (documentMetadata.ObjectTypeId is { } objectTypeId)
            {
                var objectScoreBoostMetadata = this.indexMetadata.GetObjectTypeScoreBoostMetadata(objectTypeId);
                weightedScore *= objectScoreBoostMetadata.CalculateScoreBoost(documentMetadata);
            }

            return weightedScore;
        }

        private double CalculateInverseDocumentFrequency(int matchedDocumentCount)
        {
            var idf = (this.documentCount - matchedDocumentCount + 0.5D)
                    / (matchedDocumentCount + 0.5D);

            idf = Math.Log(1D + idf);

            return idf;
        }
    }
}

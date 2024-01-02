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

        /// <inheritdoc />
        public IReadOnlyList<ScoredToken> Score(IReadOnlyList<QueryTokenMatch> tokenMatches, double weighting)
        {
            if (tokenMatches is null)
            {
                throw new ArgumentNullException(nameof(tokenMatches));
            }

            var idf = this.CalculateInverseDocumentFrequency(tokenMatches);

            return tokenMatches.Select(t =>
            {
                var documentMetadata = this.indexMetadata.GetDocumentMetadata(t.DocumentId);
                var documentTokenCounts = documentMetadata.DocumentStatistics.TokenCountByField;
                var scoredFieldMatches = new List<ScoredFieldMatch>(t.FieldMatches.Count);
                var objectScoreBoostMetadata = documentMetadata.ObjectTypeId is { } objectTypeId
                    ? this.indexMetadata.GetObjectTypeScoreBoostMetadata(objectTypeId)
                    : null;

                foreach (var fieldMatch in t.FieldMatches)
                {
                    var frequencyInDocument = fieldMatch.Locations.Count;
                    var fieldId = fieldMatch.FieldId;
                    var tokensInDocument = documentTokenCounts[fieldId];
                    var tokensInDocumentWeighting = tokensInDocument / this.averageTokenCountByField[fieldId];

                    var numerator = frequencyInDocument * this.k1PlusOne;
                    var denominator = frequencyInDocument + (this.k1 * (1 - this.b + (this.b * tokensInDocumentWeighting)));

                    var fieldScore = idf * (numerator / denominator);

                    var fieldScoreBoost = this.fieldScoreBoosts.GetScoreBoost(fieldId);

                    var weightedScore = fieldScore * weighting * fieldScoreBoost;

                    if (objectScoreBoostMetadata != null)
                    {
                        weightedScore *= objectScoreBoostMetadata.CalculateScoreBoost(documentMetadata);
                    }

                    scoredFieldMatches.Add(new ScoredFieldMatch(weightedScore, fieldMatch));
                }

                return new ScoredToken(t.DocumentId, scoredFieldMatches);
            }).ToList();
        }

        private double CalculateInverseDocumentFrequency(IReadOnlyList<QueryTokenMatch> tokens)
        {
            var tokenCount = tokens.Count;
            var idf = (this.documentCount - tokenCount + 0.5D)
                    / (tokenCount + 0.5D);

            idf = Math.Log(1D + idf);

            return idf;
        }
    }
}

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
        private readonly IItemStore snapshot;

        /// <summary>
        /// Constructs a new instance of the <see cref="OkapiBm25Scorer"/>.
        /// </summary>
        /// <param name="k1">The "k1" parameter for the scorer.</param>
        /// <param name="b">The "b" parameter for the scorer.</param>
        /// <param name="snapshot">
        /// The <see cref="IItemStore"/> of the index snapshot being queried.
        /// </param>
        internal OkapiBm25Scorer(double k1, double b, IItemStore snapshot)
        {
            if (snapshot is null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            var documentCount = (double)snapshot.Count;
            this.averageTokenCountByField = snapshot.IndexStatistics.TokenCountByField.ToDictionary(k => k.Key, k => k.Value / documentCount);
            this.documentCount = documentCount;
            this.k1 = k1;
            this.k1PlusOne = k1 + 1D;
            this.b = b;
            this.snapshot = snapshot;
        }

        /// <inheritdoc />
        public IReadOnlyList<ScoredToken> Score(IReadOnlyList<QueryTokenMatch> tokenMatches, double weighting)
        {
            if (tokenMatches is null)
            {
                throw new ArgumentNullException(nameof(tokenMatches));
            }

            var idf = CalculateInverseDocumentFrequency(tokenMatches);

            return tokenMatches.Select(t =>
            {
                var itemTokenCounts = this.snapshot.GetMetadata(t.ItemId).DocumentStatistics.TokenCountByField;
                var scoredFieldMatches = new List<ScoredFieldMatch>(t.FieldMatches.Count);
                foreach (var fieldMatch in t.FieldMatches)
                {
                    var frequencyInDocument = fieldMatch.Locations.Count;
                    var fieldId = fieldMatch.FieldId;
                    var tokensInDocument = itemTokenCounts[fieldId];
                    var tokensInDocumentWeighting = tokensInDocument / this.averageTokenCountByField[fieldId];

                    var numerator = frequencyInDocument * this.k1PlusOne;
                    var denominator = frequencyInDocument + this.k1 * (1 - this.b + this.b * tokensInDocumentWeighting);

                    var fieldScore = idf * (numerator / denominator);

                    var weightedScore = fieldScore * weighting;

                    scoredFieldMatches.Add(new ScoredFieldMatch(weightedScore, fieldMatch));
                }

                return new ScoredToken(t.ItemId, scoredFieldMatches);
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

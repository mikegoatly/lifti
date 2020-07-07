using System;
using System.Collections.Generic;
using System.Linq;

namespace Lifti.Querying
{
    public class OkapiBm25IndexScorer : IIndexScorer
    {
        private readonly Dictionary<byte, double> averageWordCountByField;
        private readonly double documentCount;
        private readonly double k1;
        private readonly double b;
        private readonly IItemStore snapshot;

        public OkapiBm25IndexScorer(double k1, double b, IItemStore snapshot)
        {
            if (snapshot is null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            var documentCount = (double)snapshot.Count;
            this.averageWordCountByField = snapshot.IndexStatistics.WordCountByField.ToDictionary(k => k.Key, k => k.Value / documentCount);
            this.documentCount = documentCount;
            this.k1 = k1;
            this.b = b;
            this.snapshot = snapshot;
        }

        public IReadOnlyList<ScoredToken> Score(IReadOnlyList<QueryWordMatch> tokenMatches)
        {
            if (tokenMatches is null)
            {
                throw new ArgumentNullException(nameof(tokenMatches));
            }

            var idf = CalculateInverseDocumentFrequency(tokenMatches);

            return tokenMatches.Select(t =>
            {
                var itemWordCounts = this.snapshot.GetMetadata(t.ItemId).DocumentStatistics.WordCountByField;
                var scoredFieldMatches = new List<ScoredFieldMatch>(t.FieldMatches.Count);
                foreach (var fieldMatch in t.FieldMatches)
                {
                    var frequencyInDocument = fieldMatch.Locations.Count;
                    var fieldId = fieldMatch.FieldId;
                    var tokensInDocument = itemWordCounts[fieldId];
                    var tokensInDocumentWeighting = tokensInDocument / this.averageWordCountByField[fieldId];

                    var numerator = frequencyInDocument * (this.k1 + 1D);
                    var denominator = frequencyInDocument + this.k1 * (1 - this.b + this.b * tokensInDocumentWeighting);

                    var fieldScore = idf * (numerator / denominator);

                    scoredFieldMatches.Add(new ScoredFieldMatch(fieldScore, fieldMatch));
                }

                return new ScoredToken(t.ItemId, scoredFieldMatches);
            }).ToList();
        }

        private double CalculateInverseDocumentFrequency(IReadOnlyList<QueryWordMatch> tokens)
        {
            var idf = (this.documentCount - tokens.Count + 0.5D)
                    / (tokens.Count + 0.5D);

            idf = Math.Log(1D + idf);

            return idf;
        }
    }
}

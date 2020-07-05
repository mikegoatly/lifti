using System;
using System.Collections.Generic;
using System.Linq;

namespace Lifti.Querying
{
    public class OkampiBm25IndexScorer : IIndexScorer
    {
        private readonly Dictionary<byte, double> averageWordCountByField;
        private readonly double documentCount;
        private readonly double k1;
        private readonly double b;
        private readonly IItemStore snapshot;

        public OkampiBm25IndexScorer(double k1, double b, IItemStore snapshot)
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

        public IReadOnlyList<ScoredToken> Score(IReadOnlyList<QueryWordMatch> tokens)
        {
            if (tokens is null)
            {
                throw new ArgumentNullException(nameof(tokens));
            }

            var documentsContainingToken = tokens.Count + 0.5D;
            var idf = Math.Log((this.documentCount - documentsContainingToken)
                            / documentsContainingToken);

            idf = Math.Max(idf, double.Epsilon);

            return tokens.Select(t =>
            {
                var itemWordCounts = this.snapshot.GetMetadata(t.ItemId).DocumentStatistics.WordCountByField;
                var itemScore = 0D;
                foreach (var fieldMatch in t.FieldMatches)
                {
                    var frequencyInDocument = fieldMatch.Locations.Count;
                    var fieldId = fieldMatch.FieldId;
                    var tokensInDocument = itemWordCounts[fieldId];
                    var tokensInDocumentWeighting = tokensInDocument / this.averageWordCountByField[fieldId];

                    var numerator = frequencyInDocument * (this.k1 + 1D);
                    var denominator = frequencyInDocument + this.k1 * (1 - this.b + this.b * tokensInDocumentWeighting);

                    var fieldScore = idf * (numerator / denominator);

                    itemScore += fieldScore;
                }

                return new ScoredToken(t, itemScore);
            }).ToList();
        }
    }
}

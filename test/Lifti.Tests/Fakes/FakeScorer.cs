using Lifti.Querying;
using System.Collections.Generic;

namespace Lifti.Tests.Fakes
{
    public class FakeScorer : IScorer
    {
        private readonly double score;

        public FakeScorer(double score)
        {
            this.score = score;
        }

        public double CalculateScore(int totalMatchedDocuments, int documentId, byte fieldId, IReadOnlyList<TokenLocation> tokenLocations, double weighting)
        {
            return this.score;
        }
    }
}

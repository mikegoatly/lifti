using Lifti.Querying;
using System.Collections.Generic;
using System.Linq;

namespace Lifti.Tests.Fakes
{
    public class FakeScorer : IScorer
    {
        private readonly double score;

        public FakeScorer(double score)
        {
            this.score = score;
        }

        public IReadOnlyList<ScoredToken> Score(IReadOnlyList<QueryTokenMatch> tokens, double weighting)
        {
            return tokens.Select(m => new ScoredToken(
                m.DocumentId,
                m.FieldMatches.Select(fm => new ScoredFieldMatch(this.score, fm)).ToList())).ToList();
        }
    }
}

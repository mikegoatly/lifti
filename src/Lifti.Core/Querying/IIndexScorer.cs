using System.Collections.Generic;

namespace Lifti.Querying
{
    public interface IIndexScorer
    {
        IReadOnlyList<ScoredToken> Score(IReadOnlyList<QueryWordMatch> tokens);
    }
}

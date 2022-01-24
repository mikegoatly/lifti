using System.Collections.Generic;

namespace Lifti.Querying
{
    /// <summary>
    /// Provides methods for scoring search results.
    /// </summary>
    public interface IScorer
    {
        /// <summary>
        /// Scores the set of <see cref="QueryTokenMatch"/> that have been matched in the document.
        /// </summary>
        /// <param name="tokens">
        /// The <see cref="QueryTokenMatch"/> instances to score.
        /// </param>
        /// <param name="weighting">
        /// The weighting multiplier to apply to the score.
        /// </param>
        /// <returns>
        /// The <see cref="ScoredToken"/> represenations of the input <paramref name="tokens"/>. There will be a 1:1
        /// mapping of input -> output and the order will be preserved.
        /// </returns>
        IReadOnlyList<ScoredToken> Score(IReadOnlyList<QueryTokenMatch> tokens, double weighting);
    }
}

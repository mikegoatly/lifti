using System.Collections.Generic;
using System.Linq;

namespace Lifti.Querying
{
    /// <summary>
    /// Provides logic for intersecting the results in two <see cref="IntermediateQueryResult"/>s.
    /// </summary>
    public class IntersectMerger : IntermediateQueryResultMerger
    {
        /// <summary>
        /// Applies the intersection to the two <see cref="IntermediateQueryResult"/>s.
        /// </summary>
        public static IEnumerable<ScoredToken> Apply(IntermediateQueryResult left, IntermediateQueryResult right)
        {
            // Swap over left and right to ensure we're performing as few iterations as possible in the intersection
            // The trade-off here is that we're building a larger dictionary
            SwapIf(left.Matches.Count > right.Matches.Count, ref left, ref right);

            var rightResults = right.Matches.ToDictionary(m => m.DocumentId);

            foreach (var leftMatch in left.Matches)
            {
                if (rightResults.TryGetValue(leftMatch.DocumentId, out var rightMatch))
                {
                    yield return new ScoredToken(
                        leftMatch.DocumentId,
                        MergeFields(leftMatch, rightMatch).ToList());
                }
            }
        }
    }
}

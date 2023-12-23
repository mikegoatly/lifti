using System.Collections.Generic;
using System.Linq;

namespace Lifti.Querying
{
    /// <summary>
    /// Provides logic for unioning the results in two <see cref="IntermediateQueryResult"/>s. The results from
    /// both parts of the query will be combined into one and field match locations combined where documents appear on both sides.
    /// </summary>
    public class UnionMerger : IntermediateQueryResultMerger
    {
        /// <summary>
        /// Applies the union operation to the <see cref="IntermediateQueryResult"/> instances.
        /// </summary>
        public static IEnumerable<ScoredToken> Apply(IntermediateQueryResult left, IntermediateQueryResult right)
        {
            // Swap over the variables to ensure we're performing as few iterations as possible in the intersection
            // "left" and "right" have no special meaning when performing an intersection
            var rightDictionary = right.Matches.ToDictionary(i => i.DocumentId);

            foreach (var leftMatch in left.Matches)
            {
                if (rightDictionary.TryGetValue(leftMatch.DocumentId, out var rightMatch))
                {
                    // Exists in both
                    yield return new ScoredToken(
                        leftMatch.DocumentId,
                        MergeFields(leftMatch, rightMatch).ToList());

                    rightDictionary.Remove(leftMatch.DocumentId);
                }
                else
                {
                    // Exists only in current
                    yield return leftMatch;
                }
            }

            // Anything still remaining in nextDictionary exist only in the new results so can just be yielded
            foreach (var rightMatch in rightDictionary.Values)
            {
                yield return rightMatch;
            }
        }
    }
}

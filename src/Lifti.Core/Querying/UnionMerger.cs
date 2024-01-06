using System.Collections.Generic;

namespace Lifti.Querying
{
    /// <summary>
    /// Provides logic for unioning the results in two <see cref="IntermediateQueryResult"/>s. The results from
    /// both parts of the query will be combined into one and field match locations combined where documents appear on both sides.
    /// </summary>
    internal sealed class UnionMerger : IntermediateQueryResultMerger
    {
        /// <summary>
        /// Applies the union operation to the <see cref="IntermediateQueryResult"/> instances.
        /// </summary>
        public static IEnumerable<ScoredToken> Apply(IntermediateQueryResult left, IntermediateQueryResult right)
        {
            // track two pointers through the lists on each side. The document ids are ordered on both sides, so we can
            // move through the lists in a single pass

            var leftIndex = 0;
            var rightIndex = 0;

            var leftMatches = left.Matches;
            var rightMatches = right.Matches;
            var leftCount = leftMatches.Count;
            var rightCount = rightMatches.Count;

            List<ScoredToken> result = new(leftCount + rightCount);

            List<ScoredFieldMatch> positionalMatches = [];
            while (leftIndex < leftCount && rightIndex < rightCount)
            {
                var leftMatch = leftMatches[leftIndex];
                var rightMatch = rightMatches[rightIndex];

                if (leftMatch.DocumentId == rightMatch.DocumentId)
                {
                    // Exists in both
                    result.Add(new ScoredToken(
                        leftMatch.DocumentId,
                        MergeFields(leftMatch, rightMatch)));

                    leftIndex++;
                    rightIndex++;
                }
                else if (leftMatch.DocumentId < rightMatch.DocumentId)
                {
                    // Exists only in current
                    result.Add(leftMatch);
                    leftIndex++;
                }
                else
                {
                    // Exists only in next
                    result.Add(rightMatch);
                    rightIndex++;
                }
            }

            // Add any remaining matches from the left
            while (leftIndex < leftCount)
            {
                result.Add(leftMatches[leftIndex]);
                leftIndex++;
            }

            // Add any remaining matches from the right
            while (rightIndex < rightCount)
            {
                result.Add(rightMatches[rightIndex]);
                rightIndex++;
            }

            return result;
        }
    }
}

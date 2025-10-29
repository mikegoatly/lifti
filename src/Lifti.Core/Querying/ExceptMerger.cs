using System.Collections.Generic;

namespace Lifti.Querying
{
    /// <summary>
    /// Provides logic for computing the set difference (exception) of two <see cref="IntermediateQueryResult"/>s.
    /// Returns all results from the left set that do NOT appear in the right set.
    /// </summary>
    internal sealed class ExceptMerger : IntermediateQueryResultMerger
    {
        /// <summary>
        /// Applies the exception operation to the two <see cref="IntermediateQueryResult"/>s.
        /// </summary>
        public static List<ScoredToken> Apply(IntermediateQueryResult left, IntermediateQueryResult right)
        {
            // Track two pointers through the lists on each side. The document ids are ordered on both sides, so we can
            // move through the lists in a single pass

            var leftIndex = 0;
            var rightIndex = 0;

            var leftMatches = left.Matches;
            var rightMatches = right.Matches;
            var leftCount = leftMatches.Count;
            var rightCount = rightMatches.Count;

            var results = new List<ScoredToken>(leftCount);

            while (leftIndex < leftCount)
            {
                var leftMatch = leftMatches[leftIndex];

                // Advance right pointer until we reach or pass the left document
                while (rightIndex < rightCount && rightMatches[rightIndex].DocumentId < leftMatch.DocumentId)
                {
                    rightIndex++;
                }

                // If right doesn't have this document, include it in results
                if (rightIndex >= rightCount || rightMatches[rightIndex].DocumentId > leftMatch.DocumentId)
                {
                    results.Add(leftMatch);
                }

                leftIndex++;
            }

            return results;
        }
    }
}

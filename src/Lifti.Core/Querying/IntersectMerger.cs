using System;
using System.Collections.Generic;
using System.Linq;

namespace Lifti.Querying
{
    /// <summary>
    /// Provides logic for intersecting the results in two <see cref="IntermediateQueryResult"/>s.
    /// </summary>
    internal sealed class IntersectMerger : IntermediateQueryResultMerger
    {
        /// <summary>
        /// Applies the intersection to the two <see cref="IntermediateQueryResult"/>s.
        /// </summary>
        public static List<ScoredToken> Apply(IntermediateQueryResult left, IntermediateQueryResult right)
        {
            // track two pointers through the lists on each side. The document ids are ordered on both sides, so we can
            // move through the lists in a single pass

            var leftIndex = 0;
            var rightIndex = 0;

            var leftMatches = left.Matches;
            var rightMatches = right.Matches;
            var leftCount = leftMatches.Count;
            var rightCount = rightMatches.Count;

            var results = new List<ScoredToken>(Math.Min(leftCount, rightCount));

            while (leftIndex < leftCount && rightIndex < rightCount)
            {
                var leftMatch = leftMatches[leftIndex];
                var rightMatch = rightMatches[rightIndex];

                if (leftMatch.DocumentId == rightMatch.DocumentId)
                {
                    results.Add(new ScoredToken(
                        leftMatch.DocumentId,
                        MergeFields(leftMatch, rightMatch)));

                    leftIndex++;
                    rightIndex++;
                }
                else if (leftMatch.DocumentId < rightMatch.DocumentId)
                {
                    leftIndex++;
                }
                else
                {
                    rightIndex++;
                }
            }

            return results;
        }
    }
}
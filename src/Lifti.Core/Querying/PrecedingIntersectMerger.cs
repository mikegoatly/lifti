using System.Collections.Generic;
using System.Linq;

namespace Lifti.Querying
{
    /// <summary>
    /// Provides logic for intersecting the results in two <see cref="IntermediateQueryResult"/>s where the fields 
    /// locations on the left must precede the matching field locations on the right.
    /// </summary>
    internal sealed class PrecedingIntersectMerger : IntermediateQueryResultMerger
    {
        /// <summary>
        /// Applies the intersection logic.
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

            List<ScoredFieldMatch> positionalMatches = [];
            while (leftIndex < leftCount && rightIndex < rightCount)
            {
                var leftMatch = leftMatches[leftIndex];
                var rightMatch = rightMatches[rightIndex];

                if (leftMatch.DocumentId == rightMatch.DocumentId)
                {
                    EnumerateFieldMatches(positionalMatches, leftMatch.FieldMatches, rightMatch.FieldMatches);
                    if (positionalMatches.Count > 0)
                    {
                        yield return new ScoredToken(leftMatch.DocumentId, positionalMatches.ToList());
                        positionalMatches.Clear();
                    }

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
        }

        private static void EnumerateFieldMatches(List<ScoredFieldMatch> fieldResults, IReadOnlyList<ScoredFieldMatch> leftFields, IReadOnlyList<ScoredFieldMatch> rightFields)
        {
            var matchedFields = JoinFields(leftFields, rightFields);

            var fieldTokenMatches = new List<ITokenLocation>();
            foreach (var (fieldId, score, leftLocations, rightLocations) in matchedFields)
            {
                var furthestRightTokenStart = rightLocations[rightLocations.Count - 1].MinTokenIndex;
                var earliestLeftTokenStart = leftLocations[0].MinTokenIndex;

                // We're only interested in tokens on the LEFT that start BEFORE the furthest RIGHT token
                // and tokens on the RIGHT that start AFTER the earliest LEFT token
                // E.g. searching "B A B A B A" with "A > B":
                // B(0) - excluded - before first A
                // A(1) - included - the first A - exists before a B
                // B(2) - included
                // A(3) - included
                // B(4) - included - the last B exists after an A
                // A(5) - excluded - does not exist before a B

                fieldTokenMatches.AddRange(
                    leftLocations.Where(l => l.MaxTokenIndex < furthestRightTokenStart)
                    .Concat(rightLocations.Where(l => l.MaxTokenIndex > earliestLeftTokenStart)));

                if (fieldTokenMatches.Count > 0)
                {
                    fieldResults.Add(
                        ScoredFieldMatch.CreateFromUnsorted(
                            score,
                            fieldId, 
                            // We need to copy the list here as we're going to reuse it
                            fieldTokenMatches));

                    fieldTokenMatches = [];
                }
            }
        }
    }
}

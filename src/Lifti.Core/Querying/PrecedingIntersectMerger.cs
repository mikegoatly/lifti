using System.Collections.Generic;
using System.Linq;

namespace Lifti.Querying
{
    /// <summary>
    /// Provides logic for intersecting the results in two <see cref="IntermediateQueryResult"/>s where the fields 
    /// locations on the left must precede the matching field locations on the right.
    /// </summary>
    public class PrecedingIntersectMerger : IntermediateQueryResultMerger
    {
        /// <summary>
        /// Applies the intersection logic.
        /// </summary>
        public static IEnumerable<ScoredToken> Apply(IntermediateQueryResult left, IntermediateQueryResult right)
        {
            // Swap over the variables to ensure we're performing as few iterations as possible in the intersection
            // "left" and "right" have no special meaning when performing an intersection
            var swapLeftAndRight = left.Matches.Count > right.Matches.Count;
            SwapIf(swapLeftAndRight, ref left, ref right);

            var rightMatches = right.Matches.ToDictionary(m => m.DocumentId);

            foreach (var leftMatch in left.Matches)
            {
                if (rightMatches.TryGetValue(leftMatch.DocumentId, out var rightMatch))
                {
                    var positionalMatches = EnumerateFieldMatches(
                        (swapLeftAndRight ? rightMatch : leftMatch).FieldMatches,
                        (swapLeftAndRight ? leftMatch : rightMatch).FieldMatches);

                    if (positionalMatches.Count > 0)
                    {
                        yield return new ScoredToken(leftMatch.DocumentId, positionalMatches);
                    }
                }
            }
        }

        private static List<ScoredFieldMatch> EnumerateFieldMatches(IReadOnlyList<ScoredFieldMatch> leftFields, IReadOnlyList<ScoredFieldMatch> rightFields)
        {
            var matchedFields = JoinFields(leftFields, rightFields);

            var fieldResults = new List<ScoredFieldMatch>(matchedFields.Count);
            var fieldTokenMatches = new List<ITokenLocationMatch>();
            foreach (var (fieldId, score, leftLocations, rightLocations) in matchedFields)
            {
                fieldTokenMatches.Clear();

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
                        new ScoredFieldMatch(
                            score,
                            new FieldMatch(fieldId, fieldTokenMatches)));
                }
            }

            return fieldResults;
        }
    }
}

﻿using System.Collections.Generic;
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

            var rightItems = right.Matches.ToDictionary(m => m.ItemId);

            foreach (var leftMatch in left.Matches)
            {
                if (rightItems.TryGetValue(leftMatch.ItemId, out var rightMatch))
                {
                    var positionalMatches = EnumerateFieldMatches(
                        (swapLeftAndRight ? rightMatch : leftMatch).FieldMatches,
                        (swapLeftAndRight ? leftMatch : rightMatch).FieldMatches);

                    if (positionalMatches.Count > 0)
                    {
                        yield return new ScoredToken(leftMatch.ItemId, positionalMatches);
                    }
                }
            }
        }

        private static IReadOnlyList<ScoredFieldMatch> EnumerateFieldMatches(IEnumerable<ScoredFieldMatch> leftFields, IEnumerable<ScoredFieldMatch> rightFields)
        {
            var matchedFields = JoinFields(leftFields, rightFields);

            var fieldResults = new List<ScoredFieldMatch>(matchedFields.Count);
            var fieldTokenMatches = new List<ITokenLocationMatch>();
            foreach (var (fieldId, score, leftLocations, rightLocations) in matchedFields)
            {
                fieldTokenMatches.Clear();

                // TODO could be optimised if order of tokens was guaranteed
                var furthestRightTokenStart = rightLocations.Max(l => l.MinTokenIndex);
                var earliestLeftTokenStart = leftLocations.Min(l => l.MinTokenIndex);

                // We're only interested in tokens on the LEFT that start BEFORE the furthest RIGHT token
                // and tokens on the RIGHT thast start AFTER the earliest LEFT token
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
                            new FieldMatch(fieldId, fieldTokenMatches.OrderBy(f => f.MinTokenIndex).ToList())));
                }
            }

            return fieldResults;
        }
    }
}

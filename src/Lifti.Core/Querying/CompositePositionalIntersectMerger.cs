using System.Collections.Generic;
using System.Linq;

namespace Lifti.Querying
{
    /// <summary>
    /// Provides logic for intersecting the results in two <see cref="IntermediateQueryResult"/>s where the fields 
    /// locations on the left must be within a specified positional tolerance of the the matching field locations on the right.
    /// </summary>
    public class CompositePositionalIntersectMerger : IntermediateQueryResultMerger
    {
        /// <summary>
        /// Applies the intersection to the <see cref="IntermediateQueryResult"/> instances.
        /// </summary>
        public static IEnumerable<ScoredToken> Apply(IntermediateQueryResult left, IntermediateQueryResult right, int leftTolerance, int rightTolerance)
        {
            // Swap over the variables to ensure we're performing as few iterations as possible in the intersection
            // Also swap the tolerance values around, otherwise we reverse the tolerance directionality.
            var swapLeftAndRight = left.Matches.Count > right.Matches.Count;
            SwapIf(swapLeftAndRight, ref left, ref right);
            SwapIf(swapLeftAndRight, ref leftTolerance, ref rightTolerance);

            var rightItems = right.Matches.ToDictionary(m => m.ItemId);

            foreach (var leftMatch in left.Matches)
            {
                if (rightItems.TryGetValue(leftMatch.ItemId, out var rightMatch))
                {
                    var positionalMatches = PositionallyMatchAndCombineTokens(
                        swapLeftAndRight,
                        leftMatch.FieldMatches,
                        rightMatch.FieldMatches,
                        leftTolerance,
                        rightTolerance);

                    if (positionalMatches.Count > 0)
                    {
                        yield return new ScoredToken(leftMatch.ItemId, positionalMatches);
                    }
                }
            }
        }

        private static IReadOnlyList<ScoredFieldMatch> PositionallyMatchAndCombineTokens(
            bool leftAndRightSwapped,
            IEnumerable<ScoredFieldMatch> leftFields,
            IEnumerable<ScoredFieldMatch> rightFields,
            int leftTolerance,
            int rightTolerance)
        {
            var matchedFields = JoinFields(leftFields, rightFields);

            var fieldResults = new List<ScoredFieldMatch>(matchedFields.Count);
            var fieldTokenMatches = new List<ITokenLocationMatch>();
            foreach (var (fieldId, score, leftLocations, rightLocations) in matchedFields)
            {
                fieldTokenMatches.Clear();

                static CompositeTokenMatchLocation CreateCompositeTokenMatchLocation(bool swapTokens, ITokenLocationMatch currentToken, ITokenLocationMatch nextToken)
                {
                    if (swapTokens)
                    {
                        return new CompositeTokenMatchLocation(nextToken, currentToken);
                    }

                    return new CompositeTokenMatchLocation(currentToken, nextToken);
                }

                int leftIndex = 0;
                int rightIndex = 0;

                while (leftIndex < leftLocations.Count && rightIndex < rightLocations.Count)
                {
                    var currentToken = leftLocations[leftIndex];
                    var nextToken = rightLocations[rightIndex];

                        if (leftTolerance > 0)
                        {
                            if ((currentToken.MinTokenIndex - nextToken.MaxTokenIndex).IsPositiveAndLessThanOrEqualTo(leftTolerance))
                            {
                                fieldTokenMatches.Add(CreateCompositeTokenMatchLocation(leftAndRightSwapped, currentToken, nextToken));
                            }
                        }

                        if (rightTolerance > 0)
                        {
                            if ((nextToken.MinTokenIndex - currentToken.MaxTokenIndex).IsPositiveAndLessThanOrEqualTo(rightTolerance))
                            {
                                fieldTokenMatches.Add(CreateCompositeTokenMatchLocation(leftAndRightSwapped, currentToken, nextToken));
                            }
                    }

                    if (currentToken.MaxTokenIndex < nextToken.MaxTokenIndex)
                    {
                        leftIndex++;
                    }
                    else
                    {
                        rightIndex++;
                    }
                }

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

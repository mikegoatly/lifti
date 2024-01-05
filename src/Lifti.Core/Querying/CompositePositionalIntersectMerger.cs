using System.Collections.Generic;
using System.Linq;

namespace Lifti.Querying
{
    /// <summary>
    /// Provides logic for intersecting the results in two <see cref="IntermediateQueryResult"/>s where the fields 
    /// locations on the left must be within a specified positional tolerance of the the matching field locations on the right.
    /// </summary>
    internal sealed class CompositePositionalIntersectMerger : IntermediateQueryResultMerger
    {
        /// <summary>
        /// Applies the intersection to the <see cref="IntermediateQueryResult"/> instances.
        /// </summary>
        public static IEnumerable<ScoredToken> Apply(IntermediateQueryResult left, IntermediateQueryResult right, int leftTolerance, int rightTolerance)
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
                    PositionallyMatchAndCombineTokens(
                        positionalMatches,
                        leftMatch.FieldMatches,
                        rightMatch.FieldMatches,
                        leftTolerance,
                        rightTolerance);

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

        private static void PositionallyMatchAndCombineTokens(
            List<ScoredFieldMatch> positionalMatches,
            IReadOnlyList<ScoredFieldMatch> leftFields,
            IReadOnlyList<ScoredFieldMatch> rightFields,
            int leftTolerance,
            int rightTolerance)
        {
            var matchedFields = JoinFields(leftFields, rightFields);

            var fieldTokenMatches = new List<ITokenLocation>();
            foreach (var (fieldId, score, leftLocations, rightLocations) in matchedFields)
            {
                var leftIndex = 0;
                var rightIndex = 0;

                while (leftIndex < leftLocations.Count && rightIndex < rightLocations.Count)
                {
                    var currentToken = leftLocations[leftIndex];
                    var nextToken = rightLocations[rightIndex];

                    if (leftTolerance > 0)
                    {
                        if ((currentToken.MinTokenIndex - nextToken.MaxTokenIndex).IsPositiveAndLessThanOrEqualTo(leftTolerance))
                        {
                            fieldTokenMatches.Add(new CompositeTokenLocation(currentToken, nextToken));
                        }
                    }

                    if (rightTolerance > 0)
                    {
                        if ((nextToken.MinTokenIndex - currentToken.MaxTokenIndex).IsPositiveAndLessThanOrEqualTo(rightTolerance))
                        {
                            fieldTokenMatches.Add(new CompositeTokenLocation(currentToken, nextToken));
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
                    positionalMatches.Add(
                        ScoredFieldMatch.CreateFromPresorted(
                            score, 
                            fieldId, 
                            fieldTokenMatches));

                    fieldTokenMatches = new();
                }
            }
        }
    }
}

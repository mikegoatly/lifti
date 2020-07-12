using System.Collections.Generic;
using System.Linq;

namespace Lifti.Querying
{
    public class CompositePositionalIntersectMerger : IntermediateQueryResultMerger
    {
        public static readonly CompositePositionalIntersectMerger Instance = new CompositePositionalIntersectMerger();

        public IEnumerable<ScoredToken> Apply(IntermediateQueryResult left, IntermediateQueryResult right, int leftTolerance, int rightTolerance)
        {
            // TODO Verify this assumption - forcing RIGHT to contain more will cause a bigger dictionary to be built
            // Swap over the variables to ensure we're performing as few iterations as possible in the intersection
            // "left" and "right" have no special meaning when performing an intersection
            SwapIf(left.Matches.Count > right.Matches.Count, ref left, ref right);

            var rightItems = right.Matches.ToDictionary(m => m.ItemId);

            foreach (var leftMatch in left.Matches)
            {
                if (rightItems.TryGetValue(leftMatch.ItemId, out var rightMatch))
                {
                    var positionalMatches = PositionallyMatchAndCombineTokens(
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
            IEnumerable<ScoredFieldMatch> leftFields,
            IEnumerable<ScoredFieldMatch> rightFields,
            int leftTolerance,
            int rightTolerance)
        {
            var matchedFields = JoinFields(leftFields, rightFields);

            var fieldResults = new List<ScoredFieldMatch>(matchedFields.Count);
            var fieldTokenMatches = new List<ITokenLocationMatch>();
            foreach (var (fieldId, score, currentLocations, nextLocations) in matchedFields)
            {
                fieldTokenMatches.Clear();

                // TODO Unoptimised O(n^2) implementation for now - big optimisations be made when location order can be guaranteed
                foreach (var currentToken in currentLocations)
                {
                    foreach (var nextToken in nextLocations)
                    {
                        if (leftTolerance > 0)
                        {
                            if ((currentToken.MinTokenIndex - nextToken.MaxTokenIndex).IsPositiveAndLessThanOrEqualTo(leftTolerance))
                            {
                                fieldTokenMatches.Add(new CompositeTokenMatchLocation(currentToken, nextToken));
                            }
                        }

                        if (rightTolerance > 0)
                        {
                            if ((nextToken.MinTokenIndex - currentToken.MaxTokenIndex).IsPositiveAndLessThanOrEqualTo(rightTolerance))
                            {
                                fieldTokenMatches.Add(new CompositeTokenMatchLocation(currentToken, nextToken));
                            }
                        }
                    }
                }

                if (fieldTokenMatches.Count > 0)
                {
                    fieldResults.Add(
                        new ScoredFieldMatch(
                            score,
                            new FieldMatch(fieldId, fieldTokenMatches.ToList())));
                }
            }

            return fieldResults;
        }
    }
}

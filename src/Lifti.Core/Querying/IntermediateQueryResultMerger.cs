using System.Collections.Generic;
using System.Linq;

namespace Lifti.Querying
{
    public abstract class IntermediateQueryResultMerger
    {
        protected static List<(
            byte fieldId,
            double score,
            IReadOnlyList<ITokenLocationMatch> leftLocations,
            IReadOnlyList<ITokenLocationMatch> rightLocations
        )>
            JoinFields(
                IEnumerable<ScoredFieldMatch> leftFields,
                IEnumerable<ScoredFieldMatch> rightFields) => leftFields.Join(
                            rightFields,
                            o => o.FieldId,
                            o => o.FieldId,
                            (inner, outer) => (
                                fieldId: inner.FieldId,
                                score: inner.Score + outer.Score,
                                leftLocations: inner.Locations,
                                rightLocations: outer.Locations))
                            .ToList();

        protected static IEnumerable<ScoredFieldMatch> MergeFields(ScoredToken leftMatch, ScoredToken rightMatch)
        {
            // We will always iterate through the total number of merged field records, so we want to optimise
            // for the smallest number of fields on the right to keep the dictionary as small as possible
            SwapIf(leftMatch.FieldMatches.Count < rightMatch.FieldMatches.Count, ref leftMatch, ref rightMatch);

            var rightFields = rightMatch.FieldMatches.ToDictionary(m => m.FieldMatch.FieldId);

            foreach (var leftField in leftMatch.FieldMatches)
            {
                if (rightFields.TryGetValue(leftField.FieldMatch.FieldId, out var rightField))
                {
                    yield return new ScoredFieldMatch(
                        leftField.Score + rightField.Score,
                        new FieldMatch(
                            leftField.FieldId,
                            leftField.Locations.Concat(rightField.Locations).OrderBy(f => f.MinTokenIndex).ToList()));

                    rightFields.Remove(leftField.FieldId);
                }
                else
                {
                    yield return leftField;
                }
            }

            // Return any remaining right fields
            foreach (var rightField in rightFields.Values)
            {
                yield return rightField;
            }
        }

        protected static void SwapIf<T>(bool condition, ref T left, ref T right)
        {
            if (condition)
            {
                var temp = left;
                left = right;
                right = temp;
            }
        }
    }
}

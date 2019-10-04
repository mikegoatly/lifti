using System.Collections.Generic;
using System.Linq;

namespace Lifti.Querying
{
    public abstract class IntermediateQueryResultMerger
    {
        protected static List<(byte FieldId, IReadOnlyList<IWordLocationMatch> leftLocations, IReadOnlyList<IWordLocationMatch> rightLocations)> 
            JoinFields(
                IEnumerable<FieldMatch> leftFields, 
                IEnumerable<FieldMatch> rightFields)
        {
            return leftFields.Join(
                            rightFields,
                            o => o.FieldId,
                            o => o.FieldId,
                            (inner, outer) => (inner.FieldId, currentLocations: inner.Locations, nextLocations: outer.Locations))
                            .ToList();
        }

        protected static IEnumerable<FieldMatch> MergeFields(QueryWordMatch leftMatch, QueryWordMatch rightMatch)
        {
            // TODO Verify this assumption - keeping the RIGHT dictionary small will cause more dictionary lookups as LEFT is iterated through
            // We will always iterate through the total number of merged field records, so we want to optimise
            // for the smallest number of fields on the right to keep the dictionary as small as possible
            SwapIf(leftMatch.FieldMatches.Count < rightMatch.FieldMatches.Count, ref leftMatch, ref rightMatch);

            var rightFields = rightMatch.FieldMatches.ToDictionary(m => m.FieldId);

            foreach (var leftField in leftMatch.FieldMatches)
            {
                if (rightFields.TryGetValue(leftField.FieldId, out var rightField))
                {
                    yield return new FieldMatch(
                        leftField.FieldId,
                        leftField.Locations.Concat(rightField.Locations).OrderBy(f => f.MinWordIndex).ToList());

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

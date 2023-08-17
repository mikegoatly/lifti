using System.Collections.Generic;
using System.Linq;

namespace Lifti.Querying
{
    /// <summary>
    /// A base helper class for classes capable of merging two <see cref="IntermediateQueryResult"/> instances.
    /// </summary>
    public abstract class IntermediateQueryResultMerger
    {
        /// <summary>
        /// Performs an inner join on two sets of field results.
        /// </summary>
        /// <returns>
        /// A list of tuples containing:
        /// * fieldId: The id of the field that matched on both sides
        /// * score: The aggregated score of the field matches
        /// * leftLocations: The locations from the left match
        /// * rightLocations: The locations from the right match
        /// </returns>
        protected static IList<(
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

        /// <summary>
        /// Merges the matched locations in two <see cref="ScoredToken"/>s. Field matches that only appear in one or the other
        /// <see cref="ScoredToken"/> are included but unaltered, field matches appearing in both <see cref="ScoredToken"/>s
        /// are unioned.
        /// </summary>
        protected static IEnumerable<ScoredFieldMatch> MergeFields(ScoredToken leftMatch, ScoredToken rightMatch)
        {
            // We will always iterate through the total number of merged field records, so we want to optimise
            // for the smallest number of fields on the right to keep the dictionary as small as possible
            SwapIf(leftMatch.FieldMatches.Count < rightMatch.FieldMatches.Count, ref leftMatch, ref rightMatch);

            var rightFields = rightMatch.FieldMatches.ToDictionary(m => m.FieldId);

            foreach (var leftField in leftMatch.FieldMatches)
            {
                if (rightFields.TryGetValue(leftField.FieldId, out var rightField))
                {
                    yield return new ScoredFieldMatch(
                        leftField.Score + rightField.Score,
                        new FieldMatch(
                            leftField.FieldId,
                            leftField.Locations.Concat(rightField.Locations)));

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

        /// <summary>
        /// A helper method to swap two fields when <paramref name="condition"/> is <c>true</c>.
        /// </summary>
        protected static void SwapIf<T>(bool condition, ref T left, ref T right)
        {
            if (condition)
            {
                (left, right) = (right, left);
            }
        }
    }
}

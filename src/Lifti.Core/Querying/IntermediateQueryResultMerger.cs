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
                IEnumerable<ScoredFieldMatch> rightFields)
        {
            return leftFields.Join(
                            rightFields,
                            o => o.FieldId,
                            o => o.FieldId,
                            (inner, outer) => (
                                fieldId: inner.FieldId,
                                score: inner.Score + outer.Score,
                                leftLocations: inner.Locations,
                                rightLocations: outer.Locations))
                            .ToList();
        }

        /// <summary>
        /// Merges the matched locations in two <see cref="ScoredToken"/>s. Field matches that only appear in one or the other
        /// <see cref="ScoredToken"/> are included but unaltered, field matches appearing in both <see cref="ScoredToken"/>s
        /// are unioned.
        /// </summary>
        protected static IEnumerable<ScoredFieldMatch> MergeFields(ScoredToken left, ScoredToken right)
        {
            var leftIndex = 0;
            var rightIndex = 0;

            var leftMatches = left.FieldMatches;
            var rightMatches = right.FieldMatches;
            var leftCount = leftMatches.Count;
            var rightCount = rightMatches.Count;

            while (leftIndex < leftCount && rightIndex < rightCount)
            {
                var leftField = leftMatches[leftIndex];
                var rightField = rightMatches[rightIndex];

                if (leftField.FieldId == rightField.FieldId)
                {
                    var concatenatedLocations = new List<ITokenLocationMatch>(leftField.Locations);
                    concatenatedLocations.AddRange(rightField.Locations);

                    yield return new ScoredFieldMatch(
                        leftField.Score + rightField.Score,
                        new FieldMatch(
                            leftField.FieldId,
                            concatenatedLocations));

                    leftIndex++;
                    rightIndex++;
                }
                else if (leftField.FieldId < rightField.FieldId)
                {
                    yield return leftField;
                    leftIndex++;
                }
                else
                {
                    yield return rightField;
                    rightIndex++;
                }
            }
        }

        /// <summary>
        /// A helper method to swap two fields when <paramref name="condition"/> is <c>true</c>.
        /// </summary>
        protected static void SwapIf<T>(bool condition, ref T left, ref T right)
        {
            if (condition)
            {
                (right, left) = (left, right);
            }
        }
    }
}

using System.Collections.Generic;

namespace Lifti.Querying
{
    /// <summary>
    /// A base helper class for classes capable of merging two <see cref="IntermediateQueryResult"/> instances.
    /// </summary>
    internal abstract class IntermediateQueryResultMerger
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
        internal static IEnumerable<(
            byte fieldId,
            double score,
            IReadOnlyList<ITokenLocation> leftLocations,
            IReadOnlyList<ITokenLocation> rightLocations
        )>
            JoinFields(
                IReadOnlyList<ScoredFieldMatch> leftFields,
                IReadOnlyList<ScoredFieldMatch> rightFields)
        {
            var leftIndex = 0;
            var rightIndex = 0;

            var leftCount = leftFields.Count;
            var rightCount = rightFields.Count;

            while (leftIndex < leftCount && rightIndex < rightCount)
            {
                var leftField = leftFields[leftIndex];
                var rightField = rightFields[rightIndex];

                if (leftField.FieldId == rightField.FieldId)
                {
                    yield return (
                        leftField.FieldId,
                        leftField.Score + rightField.Score,
                        leftField.Locations,
                        rightField.Locations);

                    leftIndex++;
                    rightIndex++;
                }
                else if (leftField.FieldId < rightField.FieldId)
                {
                    leftIndex++;
                }
                else
                {
                    rightIndex++;
                }
            }
        }

        /// <summary>
        /// Merges the matched locations in two <see cref="ScoredToken"/>s. Field matches that only appear in one or the other
        /// <see cref="ScoredToken"/> are included but unaltered, field matches appearing in both <see cref="ScoredToken"/>s
        /// are unioned.
        /// </summary>
        protected static IReadOnlyList<ScoredFieldMatch> MergeFields(ScoredToken left, ScoredToken right)
        {
            var leftIndex = 0;
            var rightIndex = 0;

            var leftMatches = left.FieldMatches;
            var rightMatches = right.FieldMatches;
            var leftCount = leftMatches.Count;
            var rightCount = rightMatches.Count;

            List<ScoredFieldMatch> results = new(leftCount + rightCount);

            while (leftIndex < leftCount && rightIndex < rightCount)
            {
                var leftField = leftMatches[leftIndex];
                var rightField = rightMatches[rightIndex];

                if (leftField.FieldId == rightField.FieldId)
                {
                    var concatenatedLocations = MergeSort(leftField.Locations, rightField.Locations);

                    results.Add(ScoredFieldMatch.CreateFromPresorted(
                        leftField.Score + rightField.Score,
                        leftField.FieldId,
                        concatenatedLocations));

                    leftIndex++;
                    rightIndex++;
                }
                else if (leftField.FieldId < rightField.FieldId)
                {
                    results.Add(leftField);
                    leftIndex++;
                }
                else
                {
                    results.Add(rightField);
                    rightIndex++;
                }
            }

            // Add any remaining matches from the left
            while (leftIndex < leftCount)
            {
                results.Add(leftMatches[leftIndex]);
                leftIndex++;
            }

            // Add any remaining matches from the right
            while (rightIndex < rightCount)
            {
                results.Add(rightMatches[rightIndex]);
                rightIndex++;
            }

            return results;
        }

        private static List<ITokenLocation> MergeSort(IReadOnlyList<ITokenLocation> left, IReadOnlyList<ITokenLocation> right)
        {
            // When merging we'll compare the values by MinTokenIndex
            var leftCount = left.Count;
            var rightCount = right.Count;
            var results = new List<ITokenLocation>(leftCount + rightCount);

            var leftIndex = 0;
            var rightIndex = 0;

            while (leftIndex < leftCount && rightIndex < rightCount)
            {
                var leftMatch = left[leftIndex];
                var rightMatch = right[rightIndex];

                if (leftMatch.MinTokenIndex < rightMatch.MinTokenIndex)
                {
                    results.Add(leftMatch);
                    leftIndex++;
                }
                else
                {
                    results.Add(rightMatch);
                    rightIndex++;
                }
            }

            // Add any remaining matches from the left
            while (leftIndex < leftCount)
            {
                results.Add(left[leftIndex]);
                leftIndex++;
            }

            // Add any remaining matches from the right
            while (rightIndex < rightCount)
            {
                results.Add(right[rightIndex]);
                rightIndex++;
            }

            return results;
        }
    }
}

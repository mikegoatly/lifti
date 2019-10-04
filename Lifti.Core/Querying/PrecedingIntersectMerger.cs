using System.Collections.Generic;
using System.Linq;

namespace Lifti.Querying
{
    public class PrecedingIntersectMerger : IntermediateQueryResultMerger
    {
        public static readonly PrecedingIntersectMerger Instance = new PrecedingIntersectMerger();

        public IEnumerable<QueryWordMatch> Apply(IntermediateQueryResult left, IntermediateQueryResult right)
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
                    var positionalMatches = EnumerateFieldMatches(
                        leftMatch.FieldMatches,
                        rightMatch.FieldMatches);

                    if (positionalMatches.Count > 0)
                    {
                        yield return new QueryWordMatch(leftMatch.ItemId, positionalMatches);
                    }
                }
            }
        }

        private static IReadOnlyList<FieldMatch> EnumerateFieldMatches(IEnumerable<FieldMatch> leftFields, IEnumerable<FieldMatch> rightFields)
        {
            var matchedFields = JoinFields(leftFields, rightFields);

            var fieldResults = new List<FieldMatch>(matchedFields.Count);
            var fieldWordMatches = new List<IWordLocationMatch>();
            foreach (var (fieldId, leftLocations, rightLocations) in matchedFields)
            {
                fieldWordMatches.Clear();

                // TODO could be optimised if order of words was guaranteed
                var furthestRightWordStart = rightLocations.Max(l => l.MinWordIndex);
                var earliestLeftWordStart = leftLocations.Min(l => l.MinWordIndex);

                // We're only interested in words on the LEFT that start BEFORE the furthest RIGHT word
                // and words on the RIGHT thast start AFTER the earliest LEFT word
                // E.g. searching "B A B A B A" with "A > B":
                // B(0) - excluded - before first A
                // A(1) - included - the first A - exists before a B
                // B(2) - included
                // A(3) - included
                // B(4) - included - the last B exists after an A
                // A(5) - excluded - does not exist before a B

                fieldWordMatches.AddRange(
                    leftLocations.Where(l => l.MaxWordIndex < furthestRightWordStart)
                    .Concat(rightLocations.Where(l => l.MaxWordIndex > earliestLeftWordStart)));

                if (fieldWordMatches.Count > 0)
                {
                    fieldResults.Add(new FieldMatch(fieldId, fieldWordMatches.OrderBy(f => f.MinWordIndex).ToList()));
                }
            }

            return fieldResults;
        }
    }
}

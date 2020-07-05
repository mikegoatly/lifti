using System.Collections.Generic;
using System.Linq;

namespace Lifti.Querying
{
    public class IntersectMerger : IntermediateQueryResultMerger
    {
        public static readonly IntersectMerger Instance = new IntersectMerger();

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
                    yield return new QueryWordMatch(
                        leftMatch.ItemId,
                        MergeFields(leftMatch, rightMatch).ToList());
                }
            }
        }
    }
}

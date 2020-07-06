using System.Collections.Generic;
using System.Linq;

namespace Lifti.Querying
{
    public class UnionMerger : IntermediateQueryResultMerger
    {
        public static readonly UnionMerger Instance = new UnionMerger();

        public IEnumerable<ScoredToken> Apply(IntermediateQueryResult left, IntermediateQueryResult right)
        {
            // TODO Verify this assumption - forcing RIGHT to contain more will cause a bigger dictionary to be built
            // Swap over the variables to ensure we're performing as few iterations as possible in the intersection
            // "left" and "right" have no special meaning when performing an intersection
            var rightDictionary = right.Matches.ToDictionary(i => i.ItemId);

            foreach (var leftMatch in left.Matches)
            {
                if (rightDictionary.TryGetValue(leftMatch.ItemId, out var rightMatch))
                {
                    // Exists in both
                    yield return new ScoredToken(
                        leftMatch.ItemId,
                        MergeFields(leftMatch, rightMatch).ToList());

                    rightDictionary.Remove(leftMatch.ItemId);
                }
                else
                {
                    // Exists only in current
                    yield return leftMatch;
                }
            }

            // Any items still remaining in nextDictionary exist only in the new results so can just be yielded
            foreach (var rightMatch in rightDictionary.Values)
            {
                yield return rightMatch;
            }
        }
    }
}

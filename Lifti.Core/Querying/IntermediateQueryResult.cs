using System.Collections.Generic;
using System.Linq;

namespace Lifti.Querying
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1815:Override equals and operator equals on value types", Justification = "Should not be compared")]
    public struct IntermediateQueryResult
    {
        public IntermediateQueryResult(IEnumerable<(int itemId, IEnumerable<IndexedWordLocation> indexedWordLocations)> matches)
        {
            this.Matches = matches;
        }

        public IEnumerable<(int itemId, IEnumerable<IndexedWordLocation> indexedWordLocations)> Matches { get; }

        /// <summary>
        /// Intersects this and the specified instance - this is the equivalent of an AND statement.
        /// </summary>
        public IntermediateQueryResult Intersect(IntermediateQueryResult results)
        {
            return new IntermediateQueryResult(IntersectEnumerator(this, results));
        }

        private static IEnumerable<(int itemId, IEnumerable<IndexedWordLocation> indexedWordLocations)> IntersectEnumerator(IntermediateQueryResult current, IntermediateQueryResult next)
        {
            var currentLookup = current.Matches.ToLookup(m => m.itemId);
            var nextLookup = next.Matches.ToLookup(m => m.itemId);

            foreach (var match in currentLookup)
            {
                if (nextLookup.Contains(match.Key))
                {
                    yield return
                        (
                        match.Key,
                        match.SelectMany(m => m.indexedWordLocations)
                            .Concat(nextLookup[match.Key].SelectMany(m => m.indexedWordLocations))
                        );
                }
            }
        }
    }
}

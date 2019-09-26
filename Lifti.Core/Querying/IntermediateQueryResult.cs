using System;
using System.Collections.Generic;
using System.Linq;

namespace Lifti.Querying
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1815:Override equals and operator equals on value types", Justification = "Should not be compared")]
    public struct IntermediateQueryResult
    {
        public static IntermediateQueryResult Empty { get; } = new IntermediateQueryResult(Array.Empty<QueryWordMatch>());

        public IntermediateQueryResult(IEnumerable<QueryWordMatch> matches)
        {
            this.Matches = matches;
        }

        public IEnumerable<QueryWordMatch> Matches { get; }

        /// <summary>
        /// Intersects this and the specified instance - this is the equivalent of an AND statement.
        /// </summary>
        public IntermediateQueryResult Intersect(IntermediateQueryResult results)
        {
            return new IntermediateQueryResult(IntersectEnumerator(this, results));
        }

        /// <summary>
        /// Union this and the specified instance - this is the equivalent of an OR statement.
        /// </summary>
        public IntermediateQueryResult Union(IntermediateQueryResult results)
        {
            return new IntermediateQueryResult(UnionEnumerator(this, results));
        }

        private static IEnumerable<QueryWordMatch> IntersectEnumerator(IntermediateQueryResult current, IntermediateQueryResult next)
        {
            var currentLookup = current.Matches.ToLookup(m => m.ItemId);
            var nextLookup = next.Matches.ToLookup(m => m.ItemId);

            foreach (var match in currentLookup)
            {
                if (nextLookup.Contains(match.Key))
                {
                    yield return new QueryWordMatch(
                        match.Key,
                        match.SelectMany(m => m.IndexedWordLocations)
                            .Concat(nextLookup[match.Key].SelectMany(m => m.IndexedWordLocations)));
                }
            }
        }

        private static IEnumerable<QueryWordMatch> UnionEnumerator(IntermediateQueryResult current, IntermediateQueryResult next)
        {
            var currentLookup = current.Matches.ToLookup(m => m.ItemId);
            var nextLookup = next.Matches.ToLookup(m => m.ItemId).ToDictionary(i => i.Key, i => i);

            foreach (var match in currentLookup)
            {
                if (nextLookup.TryGetValue(match.Key, out var nextLocations))
                {
                    // Exists in both
                    yield return new QueryWordMatch(
                        match.Key,
                        match.SelectMany(m => m.IndexedWordLocations)
                            .Concat(nextLocations.SelectMany(m => m.IndexedWordLocations)));

                    nextLookup.Remove(match.Key);
                }
                else
                {
                    // Exists only in current
                    yield return new QueryWordMatch(match.Key, match.SelectMany(m => m.IndexedWordLocations));
                }
            }

            // Any items still remaining in nextLookup exist only there
            foreach (var match in nextLookup)
            {
                yield return new QueryWordMatch(match.Key, match.Value.SelectMany(m => m.IndexedWordLocations));
            }
        }
    }
}

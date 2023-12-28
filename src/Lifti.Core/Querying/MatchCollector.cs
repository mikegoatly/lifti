using System.Collections.Generic;
using System.Linq;

namespace Lifti.Querying
{
    /// <summary>
    /// <see cref="MatchCollector"/> is used to aggregate matches from multiple <see cref="IIndexNavigator.AddExactAndChildMatches(MatchCollector)"/> 
    /// and <see cref="IIndexNavigator.AddExactMatches(MatchCollector)"/> operations. After matches have been collected,
    /// the resulting <see cref="IntermediateQueryResult"/> can be created using the <see cref="IIndexNavigator.CreateIntermediateQueryResult(MatchCollector, double)"/> method.
    /// </summary>
    public sealed class MatchCollector
    {
        internal Dictionary<int, List<IndexedToken>> CollectedMatches { get; } = [];

        /// <summary>
        /// Adds from the specified <paramref name="matches"/> to the collected matches.
        /// </summary>
        internal void Add(IEnumerable<(int documentId, IReadOnlyList<IndexedToken> indexedTokens)> matches)
        {
            foreach (var (documentId, indexedTokens) in matches)
            {
                if (this.CollectedMatches.TryGetValue(documentId, out var existingMatches))
                {
                    existingMatches.AddRange(indexedTokens);
                }
                else
                {
                    if (matches is List<IndexedToken> indexedTokensList)
                    {
                        this.CollectedMatches[documentId] = indexedTokensList;
                    }
                    else
                    {
                        this.CollectedMatches[documentId] = new(indexedTokens);
                    }
                }   
            }
        }
    }
}

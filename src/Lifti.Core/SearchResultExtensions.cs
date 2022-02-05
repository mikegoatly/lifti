using System.Collections.Generic;
using System.Linq;

namespace Lifti
{
    /// <summary>
    /// Extension methods for LIFTI search results.
    /// </summary>
    public static class SearchResultExtensions
    {
        /// <summary>
        /// Re-orders the search results, only considering scores for a single field.
        /// </summary>
        /// <param name="searchResults">The search results to re-order.</param>
        /// <param name="fieldName">The name of the field to order the search results by.</param>
        public static IEnumerable<SearchResult<TKey>> OrderByField<TKey>(this IEnumerable<SearchResult<TKey>> searchResults, string fieldName)
        {
            return searchResults.OrderByDescending(
                r => r.FieldMatches.Sum(f => f.FoundIn == fieldName ? f.Score : 0D));
        }
    }
}

using Lifti.Querying.QueryParts;
using System.Collections.Generic;

namespace Lifti.Querying
{
    /// <summary>
    /// Describes a parsed query that can be executed against an index.
    /// </summary>
    public interface IQuery
    {
        /// <summary>
        /// The root query part.
        /// </summary>
        IQueryPart Root { get; }

        /// <summary>
        /// Executes the query defined by this instance against an index.
        /// </summary>
        /// <typeparam name="TKey">
        /// The type of the key in the index.
        /// </typeparam>
        /// <param name="index">
        /// The <see cref="IndexSnapshot{TKey}"/> to execute against.
        /// </param>
        /// <returns>
        /// An enumerable of <see cref="SearchResult{T}"/> instances that matched the query.
        /// </returns>
        IEnumerable<SearchResult<TKey>> Execute<TKey>(IIndexSnapshot<TKey> index);
    }
}

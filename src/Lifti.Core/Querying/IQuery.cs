using Lifti.Querying.QueryParts;

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
        /// The <see cref="FullTextIndex{TKey}"/> to execute against.
        /// </param>
        /// <param name="options">
        /// Optional execution options to control the execution of the query.
        /// </param>
        /// <returns>
        /// The <see cref="SearchResults{TKey}"/> containing the results of the query.
        /// </returns>
        ISearchResults<TKey> Execute<TKey>(FullTextIndex<TKey> index, QueryExecutionOptions? options = null)
            where TKey : notnull;
    }
}

using Lifti.Querying;
using Lifti.Tokenization;
using System;

namespace Lifti
{
    /// <summary>
    /// Extension methods for <see cref="IFullTextIndex{TKey}"/> implementations.
    /// </summary>
    public static class FullTextIndexExtensions
    {
        /// <summary>
        /// Parses the given <paramref name="queryText"/> using the index's <see cref="IQueryParser"/>
        /// and default <see cref="IIndexTokenizer"/>.
        /// </summary>
        public static IQuery ParseQuery<TKey>(this IFullTextIndex<TKey> index, string queryText)
        {
            return index is null
                ? throw new ArgumentNullException(nameof(index))
                : index.QueryParser.Parse(index.FieldLookup, queryText, index);
        }

        /// <summary>
        /// Creates a new fluent query builder against the index.
        /// <code>
        /// var results = index.Query().ExactMatch("hello").And.ExactMatch("world").Execute();
        /// </code>
        /// </summary>
        public static SearchTermFluentQueryBuilder<TKey> Query<TKey>(this IFullTextIndex<TKey> index)
            where TKey : notnull
        {
            return FluentQueryBuilder<TKey>.StartQuery(index);
        }
    }
}

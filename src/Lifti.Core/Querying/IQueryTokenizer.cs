using Lifti.Tokenization;
using System.Collections.Generic;

namespace Lifti.Querying
{
    /// <summary>
    /// Provides methods for tokenizing a textual query into <see cref="QueryToken"/>s.
    /// </summary>
    internal interface IQueryTokenizer
    {
        /// <summary>
        /// Parses the given <paramref name="queryText"/> into its respective <see cref="QueryToken"/> parts.
        /// </summary>
        /// <param name="queryText">
        /// The text to parse.
        /// </param>
        /// <param name="tokenizerProvider">
        /// The <see cref="IIndexTokenizerProvider"/> used to access the various <see cref="IIndexTokenizer"/> instances associated to the index.
        /// </param>
        /// <returns>
        /// The parsed <see cref="QueryToken"/>s.
        /// </returns>
        IEnumerable<QueryToken> ParseQueryTokens(string queryText, IIndexTokenizerProvider tokenizerProvider);
    }
}
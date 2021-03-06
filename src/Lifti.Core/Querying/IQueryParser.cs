﻿using Lifti.Tokenization;

namespace Lifti.Querying
{
    /// <summary>
    /// Provides methods for parsing a query represented as text to an <see cref="IQuery"/>.
    /// </summary>
    public interface IQueryParser
    {
        /// <summary>
        /// Parses the<see cref="IQuery"/> implementation that represents the <paramref name="queryText"/>. The value
        /// in <paramref name="queryText"/> is tokenized using the provided <see cref="ITokenizer"/>.
        /// </summary>
        /// <param name="fieldLookup">
        /// The <see cref="IIndexedFieldLookup"/> to used to obtain information about fields
        /// referenced in the query.
        /// </param>
        /// <param name="queryText">
        /// The text of the query to parse.
        /// </param>
        /// <param name="tokenizer">
        /// The <see cref="ITokenizer"/> to use when parsing tokens out of the <paramref name="queryText"/>.
        /// </param>
        /// <returns>
        /// The parsed <see cref="IQuery"/> representation of the query.
        /// </returns>
        IQuery Parse(IIndexedFieldLookup fieldLookup, string queryText, ITokenizer tokenizer);
    }
}

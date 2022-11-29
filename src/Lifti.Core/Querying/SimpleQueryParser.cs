using Lifti.Querying.QueryParts;
using Lifti.Tokenization;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Lifti.Querying
{
    /// <summary>
    /// A basic implementation of a query parser that combines all search terms with either an "and" or an "or". Used as an alternative
    /// to the complex default LIFTI query parser when you don't need the full complexity of the query language.
    /// </summary>
    public class SimpleQueryParser : IQueryParser
    {
        private readonly QueryParserOptions options;

        /// <summary>
        /// Initializes a new instance of <see cref="SimpleQueryParser"/>.
        /// </summary>
        /// <param name="options">
        /// The options that will be used to control the behavior of the query parser.
        /// </param>
        public SimpleQueryParser(QueryParserOptions options)
        {
            this.options = options;
        }

        /// <inheritdoc />
        public IQuery Parse(IIndexedFieldLookup fieldLookup, string queryText, IIndexTokenizerProvider tokenizerProvider)
        {
            if (queryText is null)
            {
                throw new ArgumentNullException(nameof(queryText));
            }

            if (tokenizerProvider is null)
            {
                throw new ArgumentNullException(nameof(tokenizerProvider));
            }

            var tokens = tokenizerProvider.DefaultTokenizer.Process(queryText.AsSpan());
            if (tokens.Count == 0)
            {
                return Query.Empty;
            }

            // Join all the tokens together with the appropriate operators
            var combined = this.options.DefaultJoiningOperator switch
            {
                QueryTermJoinOperatorKind.And => AndQueryOperator.CombineAll(CreateSearchTermTokens(tokens)),
                QueryTermJoinOperatorKind.Or => OrQueryOperator.CombineAll(CreateSearchTermTokens(tokens)),
                _ => throw new QueryParserException(ExceptionMessages.UnsupportedQueryJoiningOperator, this.options.DefaultJoiningOperator)
            };

            // Compose the final query
            return new Query(combined);
        }

        private IEnumerable<IQueryPart> CreateSearchTermTokens(IReadOnlyList<Token> tokens)
        {
            if (this.options.AssumeFuzzySearchTerms)
            {
                return tokens.Select(
                    x => new FuzzyMatchQueryPart(
                        x.Value,
                        this.options.FuzzySearchMaxEditDistance(x.Value.Length),
                        this.options.FuzzySearchMaxSequentialEdits(x.Value.Length)));
            }

            return tokens.Select(x => new ExactWordQueryPart(x.Value));
        }
    }
}

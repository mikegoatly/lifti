using Lifti.Tokenization;
using System;

namespace Lifti.Querying
{
    /// <summary>
    /// A builder capable of creating an <see cref="IQueryParser"/> instance for use in an index.
    /// </summary>
    public class QueryParserBuilder
    {
        private static readonly Func<QueryParserOptions, IQueryParser> defaultQueryParserFactory = o => new QueryParser(o);
        private Func<QueryParserOptions, IQueryParser> factory = defaultQueryParserFactory;

        private bool assumeFuzzySearchTerms;

        /// <summary>
        /// Configures the tokenizer so that it will always treat search terms as fuzzy search expressions.
        /// When this is set, it is not necessary to prefix search terms with "?" to indicate a fuzzy search.
        /// </summary>
        public QueryParserBuilder AssumeFuzzySearchTerms(bool fuzzySearchByDefault = true)
        {
            this.assumeFuzzySearchTerms = fuzzySearchByDefault;
            return this;
        }

        /// <summary>
        /// Configures a factory method capable of creating an implementation of <see cref="IQueryParser"/>
        /// with the provided options.
        /// </summary>
        public QueryParserBuilder WithQueryParserFactory(Func<QueryParserOptions, IQueryParser> factory)
        {
            this.factory = factory ?? throw new ArgumentNullException(nameof(factory));
            return this;
        }

        /// <summary>
        /// Builds an <see cref="ITokenizer"/> instance matching the current configuration.
        /// </summary>
        public IQueryParser Build()
        {
            var options = new QueryParserOptions()
            {
                AssumeFuzzySearchTerms = this.assumeFuzzySearchTerms
            };

            return this.factory(options);
        }
    }
}

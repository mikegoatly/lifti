using Lifti.Querying.QueryParts;
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
        private QueryTermJoinOperatorKind defaultJoiningOperator = QueryTermJoinOperatorKind.And;
        private bool fuzzySearchByDefault;
        private Func<int, ushort>? fuzzySearchMaxEditDistance;
        private Func<int, ushort>? fuzzySearchMaxSequentialEdits;

        internal QueryParserBuilder()
        {
        }

        internal QueryParserBuilder(Func<QueryParserOptions, IQueryParser> queryParserFactory)
        {
            this.factory = queryParserFactory;
        }


        /// <summary>
        /// Configures the tokenizer so that it will always treat search terms as fuzzy search expressions.
        /// When this is set, it is not necessary to prefix search terms with "?" to indicate a fuzzy search.
        /// </summary>
        public QueryParserBuilder AssumeFuzzySearchTerms(bool fuzzySearchByDefault = true)
        {
            this.fuzzySearchByDefault = fuzzySearchByDefault;
            return this;
        }

        /// <summary>
        /// Configures the default parameters for a fuzzy search when not provided explicitly as part of the query.
        /// </summary>
        /// <param name="maxEditDistance">The maximum of edits allowed for any given match. The higher this value, the more divergent 
        /// matches will be.</param>
        /// <param name="maxSequentialEdits">The maximum number of edits that are allowed to appear sequentially. By default this is 1,
        /// which forces matches to be more similar to the search criteria.</param>
        public QueryParserBuilder WithFuzzySearchDefaults(ushort maxEditDistance = FuzzyMatchQueryPart.DefaultMaxEditDistance, ushort maxSequentialEdits = FuzzyMatchQueryPart.DefaultMaxSequentialEdits)
        {
            this.fuzzySearchMaxEditDistance = termLength => maxEditDistance;
            this.fuzzySearchMaxSequentialEdits = termLength => maxSequentialEdits;
            return this;
        }

        /// <summary>
        /// Configures the default parameters for a fuzzy search when not provided explicitly as part of the query.
        /// </summary>
        /// <param name="maxEditDistance">A function that can derive the maximum of edits allowed for a query term of a given length. The higher this value, the more divergent 
        /// matches will be.</param>
        /// <param name="maxSequentialEdits">A function that can derive the maximum number of edits that are allowed to appear sequentially for a query term of a given length.</param>
        public QueryParserBuilder WithFuzzySearchDefaults(Func<int, ushort> maxEditDistance, Func<int, ushort> maxSequentialEdits)
        {
            this.fuzzySearchMaxEditDistance = maxEditDistance;
            this.fuzzySearchMaxSequentialEdits = maxSequentialEdits;
            return this;
        }

        /// <summary>
        /// The joining operator that should be used 
        /// </summary>
        /// <param name="joiningOperator"></param>
        /// <returns></returns>
        public QueryParserBuilder WithDefaultJoiningOperator(QueryTermJoinOperatorKind joiningOperator = QueryTermJoinOperatorKind.And)
        {
            this.defaultJoiningOperator = joiningOperator;
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
            var options = new QueryParserOptions
            {
                AssumeFuzzySearchTerms = this.fuzzySearchByDefault,
                DefaultJoiningOperator = this.defaultJoiningOperator
            };

            if (this.fuzzySearchMaxEditDistance != null)
            {
                options.FuzzySearchMaxEditDistance = this.fuzzySearchMaxEditDistance;
            }

            if (this.fuzzySearchMaxSequentialEdits != null)
            {
                options.FuzzySearchMaxSequentialEdits = this.fuzzySearchMaxSequentialEdits;
            }

            return this.factory(options);
        }
    }
}

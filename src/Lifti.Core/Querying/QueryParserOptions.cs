namespace Lifti.Querying
{
    /// <summary>
    /// Defines options that can be applied to the standard LIFTI query parser.
    /// </summary>
    public class QueryParserOptions
    {
        internal QueryParserOptions()
        {
        }

        /// <summary>
        /// Gets a value indicating whether search terms will always be treated as fuzzy search expressions.
        /// When this is set, it is not necessary to prefix search terms with "?" to indicate a fuzzy search.
        /// </summary>
        public bool AssumeFuzzySearchTerms { get; internal set; }
    }

}

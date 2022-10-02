using System;

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

        /// <summary>
        /// Gets a function capable of deriving the maximum of edits allowed for a fuzzy search term of a given length.
        /// </summary>
        public Func<int, ushort> FuzzySearchMaxEditDistance { get; internal set; } = static termLength => 4;

        /// <summary>
        /// Gets a function capable of deriving the maximum number of edits that are allowed to appear sequentially for a fuzzy search term of a given length.
        /// </summary>
        public Func<int, ushort> FuzzySearchMaxSequentialEdits { get; internal set; } = static termLength => 1;
    }

}

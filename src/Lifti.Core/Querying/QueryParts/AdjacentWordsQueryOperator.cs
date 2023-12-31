using System;
using System.Collections.Generic;

namespace Lifti.Querying.QueryParts
{
    /// <summary>
    /// A query part requiring that a series of words must appear in a document in sequence.
    /// </summary>
    public class AdjacentWordsQueryOperator : IQueryPart
    {
        /// <summary>
        /// Constructs a new <see cref="AdjacentWordsQueryOperator"/> instance.
        /// </summary>
        /// <param name="words">
        /// The <see cref="IQueryPart"/>s that must appear in sequence.
        /// </param>
        public AdjacentWordsQueryOperator(IReadOnlyList<IQueryPart> words)
        {
            this.Words = words;
        }

        /// <summary>
        /// Gets the <see cref="IQueryPart"/>s that must appear in sequence.
        /// </summary>
        public IReadOnlyList<IQueryPart> Words { get; }

        /// <inheritdoc/>
        public IntermediateQueryResult Evaluate(Func<IIndexNavigator> navigatorCreator, QueryContext queryContext)
        {
            var i = 0;
            var results = IntermediateQueryResult.Empty;
            do
            {
                var nextResults = this.Words[i].Evaluate(navigatorCreator, queryContext);
                if (results.Matches.Count == 0)
                {
                    // Special case the first word, as we don't want to intersect with the initial empty set
                    results = nextResults;
                }
                else
                {
                    results = results.CompositePositionalIntersect(nextResults, 0, 1);
                }

                i++;

            } while (i < this.Words.Count && results.Matches.Count > 0);

            return results;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return "\"" + string.Join(" ", this.Words) + "\"";
        }
    }
}

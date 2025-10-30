using System;
using System.Collections.Generic;

namespace Lifti.Querying.QueryParts
{
    /// <summary>
    /// A query part requiring that a series of words must appear in a document in sequence.
    /// </summary>
    public sealed class AdjacentWordsQueryOperator : IQueryPart
    {
        private double? weighting;

        /// <summary>
        /// Constructs a new <see cref="AdjacentWordsQueryOperator"/> instance.
        /// </summary>
        /// <param name="words">
        /// The <see cref="IQueryPart"/>s that must appear in sequence.
        /// </param>
        public AdjacentWordsQueryOperator(IReadOnlyList<IQueryPart> words)
        {
            ArgumentNullException.ThrowIfNull(words);

            if (words.Count == 0)
            {
                throw new LiftiException(ExceptionMessages.EmptyAdjacentWordsQueryPart);
            }

            this.Words = words;
        }

        /// <summary>
        /// Gets the <see cref="IQueryPart"/>s that must appear in sequence.
        /// </summary>
        public IReadOnlyList<IQueryPart> Words { get; }

        /// <inheritdoc/>
        public IntermediateQueryResult Evaluate(Func<IIndexNavigator> navigatorCreator, QueryContext queryContext)
        {
            ArgumentNullException.ThrowIfNull(queryContext);

            var i = 0;
            var results = IntermediateQueryResult.Empty;

            var parentQueryContext = queryContext;
            
            var childQueryContext = queryContext;

            do
            {
                var nextResults = this.Words[i].Evaluate(navigatorCreator, childQueryContext);
                if (results.Matches.Count == 0)
                {
                    // Special case the first word, as we don't want to intersect with the initial empty set
                    results = nextResults;
                }
                else
                {
                    var timing = parentQueryContext.ExecutionTimings.Start(this, parentQueryContext);
                    results = results.CompositePositionalIntersect(nextResults, 0, 1);
                    timing.Complete(results);
                }

                // Filter subsequent words to only those that match the document ids we have so far
                childQueryContext = childQueryContext with { FilterToDocumentIds = results.ToDocumentIdLookup() };

                i++;

            } while (i < this.Words.Count && results.Matches.Count > 0);

            return results;
        }

        /// <inheritdoc/>
        public double CalculateWeighting(Func<IIndexNavigator> navigatorCreator)
        {
            // Each result is intersected in sequence, so we approximate the total weighting by calculating
            // the first and dividing by the number of words
            this.weighting ??= this.Words[0].CalculateWeighting(navigatorCreator) / this.Words.Count;
            return this.weighting.GetValueOrDefault();
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return "\"" + string.Join(" ", this.Words) + "\"";
        }
    }
}

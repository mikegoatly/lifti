using System;
using System.Collections.Generic;
using System.Linq;

namespace Lifti
{
    /// <summary>
    /// Returned as results from an index query execution.
    /// </summary>
    /// <typeparam name="TKey">
    /// The type of key in the index.
    /// </typeparam>
    public class SearchResult<TKey>
    {
        /// <summary>
        /// Constructs a new <see cref="SearchResult{TKey}"/> instance.
        /// </summary>
        public SearchResult(TKey key, IReadOnlyList<FieldSearchResult> locations)
        {
            this.Key = key;
            this.FieldMatches = locations ?? throw new ArgumentNullException(nameof(locations));

            var score = 0D;
            for (var i = 0; i < locations.Count; i++)
            {
                score += locations[i].Score;
            }

            this.Score = score;
        }

        /// <summary>
        /// Gets the key of the document that matched the search criteria.
        /// </summary>
        public TKey Key { get; }

        /// <summary>
        /// Gets the fields that were matched for the document. Each of these is scored independently and provides detailed information
        /// about the location of the tokens that were matched.
        /// </summary>
        public IReadOnlyList<FieldSearchResult> FieldMatches { get; }

        /// <summary>
        /// Gets the overall score for this match. This is a sum of the scores for this instance's <see cref="FieldMatches"/>.
        /// </summary>
        public double Score { get; }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{this.Key}{Environment.NewLine}{string.Join(Environment.NewLine, this.FieldMatches.Select(l => "  " + l.ToString()))}";
        }
    }
}

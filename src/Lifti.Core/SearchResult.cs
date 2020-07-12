using System;
using System.Collections.Generic;
using System.Linq;

namespace Lifti
{
    public class SearchResult<T>
    {
        public SearchResult(T item, IReadOnlyList<FieldSearchResult> locations)
        {
            this.Key = item;
            this.FieldMatches = locations ?? throw new ArgumentNullException(nameof(locations));

            var score = 0D;
            for (var i = 0; i < locations.Count; i++)
            {
                score += locations[i].Score;
            }

            this.Score = score;
        }

        /// <summary>
        /// Gets the item that matched the search criteria.
        /// </summary>
        public T Key { get; }

        /// <summary>
        /// Gets the fields that were matched for the item. Each of these is scored independently and provides detailed information
        /// about the location of the tokens that were matched.
        /// </summary>
        public IReadOnlyList<FieldSearchResult> FieldMatches { get; }

        /// <summary>
        /// Gets the overall score for this match. This is a sum of the scores for this instance's <see cref="FieldMatches"/>.
        /// </summary>
        public double Score { get; }

        public override string ToString()
        {
            return $"{this.Key}{Environment.NewLine}{string.Join(Environment.NewLine, this.FieldMatches.Select(l => "  " + l.ToString()))}";
        }
    }
}

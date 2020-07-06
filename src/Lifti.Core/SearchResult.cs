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

        public T Key { get; }
        public IReadOnlyList<FieldSearchResult> FieldMatches { get; }
        public double Score { get; }

        public override string ToString()
        {
            return $"{this.Key}{Environment.NewLine}{string.Join(Environment.NewLine, this.FieldMatches.Select(l => "  " + l.ToString()))}";
        }
    }
}

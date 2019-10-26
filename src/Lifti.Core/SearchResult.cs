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
            this.FieldMatches = locations;
        }

        public T Key { get; set; }
        public IReadOnlyList<FieldSearchResult> FieldMatches { get; set; }

        public override string ToString()
        {
            return $"{this.Key}{Environment.NewLine}{string.Join(Environment.NewLine, this.FieldMatches.Select(l => "  " + l.ToString()))}";
        }
    }
}

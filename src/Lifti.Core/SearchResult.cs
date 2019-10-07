using System;
using System.Collections.Generic;
using System.Linq;

namespace Lifti
{
    public class SearchResult<T>
    {
        public SearchResult(T item, IReadOnlyList<FieldSearchResult> locations)
        {
            this.Item = item;
            this.FieldMatches = locations;
        }

        public T Item { get; set; }
        public IReadOnlyList<FieldSearchResult> FieldMatches { get; set; }

        public override string ToString()
        {
            return $"{this.Item}{Environment.NewLine}{string.Join(Environment.NewLine, this.FieldMatches.Select(l => "  " + l.ToString()))}";
        }
    }
}

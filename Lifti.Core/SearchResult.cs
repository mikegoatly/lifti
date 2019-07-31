using System;
using System.Collections.Generic;
using System.Linq;

namespace Lifti
{
    public class SearchResult<T>
    {
        public SearchResult(T item, IReadOnlyList<MatchedLocation> locations)
        {
            this.Item = item;
            this.Locations = locations;
        }

        public T Item { get; set; }
        public IReadOnlyList<MatchedLocation> Locations { get; set; }

        public override string ToString()
        {
            return $"{this.Item}{Environment.NewLine}{string.Join(Environment.NewLine, this.Locations.Select(l => "  " + l.ToString()))}";
        }
    }
}

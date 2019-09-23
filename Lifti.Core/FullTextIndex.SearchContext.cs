using System;
using System.Collections.Generic;
using System.Linq;

namespace Lifti
{
    public partial class FullTextIndex<TKey>
    {
        internal class SearchContext
        {
            private readonly FullTextIndex<TKey> index;
            private readonly Dictionary<int, List<IndexedWordLocation>> results = new Dictionary<int, List<IndexedWordLocation>>();

            public SearchContext(FullTextIndex<TKey> index)
            {
                this.index = index;
            }

            public void Reset()
            {
            }

            public void Match(ReadOnlySpan<char> text)
            {
                var navigator = new IndexNavigator(index.Root);
                foreach (var character in text)
                {
                    if (!navigator.Process(character))
                    {
                        return;
                    }
                }

                this.AddMatches(navigator.GetExactMatches());
            }

            private void AddMatches(IEnumerable<(int itemId, IReadOnlyList<IndexedWordLocation> indexedWordLocations)> matches)
            {
                foreach (var (itemId, indexedWordLocations) in matches)
                {
                    if (!this.results.TryGetValue(itemId, out var itemResults))
                    {
                        itemResults = new List<IndexedWordLocation>();
                        this.results[itemId] = itemResults;
                    }

                    itemResults.AddRange(indexedWordLocations);
                }
            }

            public IEnumerable<SearchResult<TKey>> Results()
            {
                foreach (var itemResults in this.results)
                {
                    var item = this.index.idPool.GetItemForId(itemResults.Key);
                    yield return new SearchResult<TKey>(
                        item,
                        itemResults.Value.Select(m => new MatchedLocation(this.index.fieldLookup.GetFieldForId(m.FieldId), m.Locations)).ToList());
                }
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;

namespace Lifti.Querying
{
    public class Query : IQuery
    {
        public Query(IQueryPart root)
        {
            this.Root = root;
        }

        public IQueryPart Root { get; }

        public IEnumerable<SearchResult<TKey>> Execute<TKey>(IFullTextIndex<TKey> index)
        {
            if (index is null)
            {
                throw new ArgumentNullException(nameof(index));
            }

            if (this.Root == null)
            {
                yield break;
            }

            var matches = this.Root.Evaluate(() => new IndexNavigator(index.Root)).Matches;
            var results = new Dictionary<int, List<IndexedWord>>();

            foreach (var match in matches)
            {
                if (!results.TryGetValue(match.ItemId, out var itemResults))
                {
                    itemResults = new List<IndexedWord>();
                    results[match.ItemId] = itemResults;
                }

                itemResults.AddRange(match.IndexedWordLocations);
            }

            foreach (var itemResults in matches)
            {
                var item = index.IdPool.GetItemForId(itemResults.ItemId);
                yield return new SearchResult<TKey>(
                    item,
                    itemResults.IndexedWordLocations.Select(m => new MatchedLocation(index.FieldLookup.GetFieldForId(m.FieldId), m.Locations)).ToList());
            }
        }
    }

}

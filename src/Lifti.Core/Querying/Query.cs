using Lifti.Querying.QueryParts;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Lifti.Querying
{
    public class Query : IQuery
    {
        /// <summary>
        /// Constructs a new <see cref="Query"/> object capable of searching against an index.
        /// </summary>
        /// <param name="root">
        /// The root part of the query. Passing this parameter as null is allowed, and causes the query
        /// to always return an empty set of results.
        /// </param>
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

            var matches = this.Root.Evaluate(index.CreateNavigator).Matches;
            var results = new Dictionary<int, List<FieldMatch>>();

            foreach (var match in matches)
            {
                if (!results.TryGetValue(match.ItemId, out var itemResults))
                {
                    itemResults = new List<FieldMatch>();
                    results[match.ItemId] = itemResults;
                }

                itemResults.AddRange(match.FieldMatches);
            }

            foreach (var itemResults in matches)
            {
                var item = index.IdLookup.GetItemForId(itemResults.ItemId);
                yield return new SearchResult<TKey>(
                    item,
                    itemResults.FieldMatches.Select(m => new FieldSearchResult(
                        index.FieldLookup.GetFieldForId(m.FieldId),
                        m.GetWordLocations()))
                    .ToList());
            }
        }
    }

}

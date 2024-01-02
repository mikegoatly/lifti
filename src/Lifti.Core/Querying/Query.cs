using Lifti.Querying.QueryParts;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Lifti.Querying
{
    /// <inheritdoc />
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

        /// <inheritdoc />
        public IQueryPart Root { get; }

        /// <summary>
        /// Gets an empty query.
        /// </summary>
        public static IQuery Empty { get; } = new Query(EmptyQueryPart.Instance);

        /// <inheritdoc />
        public IEnumerable<SearchResult<TKey>> Execute<TKey>(IIndexSnapshot<TKey> index)
        {
            if (index is null)
            {
                throw new ArgumentNullException(nameof(index));
            }

            if (this.Root == EmptyQueryPart.Instance)
            {
                return Enumerable.Empty<SearchResult<TKey>>();
            }

            var indexMetadata = index.Metadata;
            var fieldLookup = index.FieldLookup;
            var evaluationResult = this.Root.Evaluate(index.CreateNavigator, QueryContext.Empty);
            var matches = evaluationResult.Matches;
            var results = new Dictionary<int, List<ScoredFieldMatch>>();

            foreach (var match in matches)
            {
                if (!results.TryGetValue(match.DocumentId, out var documentResults))
                {
                    documentResults = [];
                    results[match.DocumentId] = documentResults;
                }

                documentResults.AddRange(match.FieldMatches);
            }

            var searchResults = new List<SearchResult<TKey>>(matches.Count);
            foreach (var documentResults in matches)
            {
                var documentMetadata = indexMetadata.GetDocumentMetadata(documentResults.DocumentId);

                searchResults.Add(
                    new SearchResult<TKey>(
                        documentMetadata.Key,
                        documentResults.FieldMatches.Select(m => new FieldSearchResult(
                            fieldLookup.GetFieldForId(m.FieldId),
                            m.Score,
                            m.FieldMatch.GetTokenLocations()))
                        .ToList()));
            }

            return searchResults.OrderByDescending(r => r.Score);
        }

        /// <inheritdoc />
        public override string? ToString()
        {
            return this.Root.ToString();
        }
    }

}

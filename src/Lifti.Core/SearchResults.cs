using Lifti.Querying;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Lifti
{
    /// <summary>
    /// Contains search results returned from an index, with additional operations that can be performed, 
    /// e.g. re-ordering them and extracting search phrases from the original source text.
    /// </summary>
    internal record SearchResults<TKey> : ISearchResults<TKey>
        where TKey : notnull
    {
        private readonly FullTextIndex<TKey> index;
        private readonly Lazy<QueryExecutionPlan> executionPlan;

        internal SearchResults(
            FullTextIndex<TKey> index,
            IEnumerable<SearchResult<TKey>> searchResults,
            ExecutionTimings? executionTimings)
        {
            var searchResultsList = searchResults as IReadOnlyCollection<SearchResult<TKey>> ?? searchResults.ToList();
            this.Results = searchResultsList;
            this.index = index;
            this.executionPlan = new Lazy<QueryExecutionPlan>(
                () => new QueryExecutionPlan(searchResultsList.Count, executionTimings ?? new ExecutionTimings()));
        }

        private IReadOnlyCollection<SearchResult<TKey>> Results { get; init; }

        /// <inheritdoc />
        public int Count => this.Results.Count;

        /// <inheritdoc />
        public ISearchResults<TKey> OrderByField(string fieldName)
        {
            return this with
            {
                Results = this.Results.OrderBy(r => r.FieldMatches.Sum(f => f.FoundIn == fieldName ? f.Score : 0D)).ToList()
            };
        }

        /// <inheritdoc />
        public QueryExecutionPlan GetExecutionPlan()
        {
            return this.executionPlan.Value;
        }

        /// <inheritdoc />
        public async Task<IEnumerable<DocumentPhrases<TKey, TObject>>> CreateMatchPhrasesAsync<TObject>(
            Func<IReadOnlyList<TKey>, IReadOnlyList<TObject>> loadItems,
            CancellationToken cancellationToken = default)
        {
            return await this.CreateMatchPhrasesAsync(
                (keys, ct) => new ValueTask<IReadOnlyList<TObject>>(loadItems(keys)),
                cancellationToken)
                .ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<IEnumerable<DocumentPhrases<TKey, TObject>>> CreateMatchPhrasesAsync<TObject>(
            Func<IReadOnlyList<TKey>, CancellationToken, ValueTask<IReadOnlyList<TObject>>> loadItemsAsync,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(loadItemsAsync);

            var itemTokenization = this.index.ObjectTypeConfiguration.Get<TObject>();
            var filteredResults = this.FilterFieldMatches<TObject>();

            var objectResults = (await loadItemsAsync(filteredResults.Select(x => x.searchResult.Key).ToList(), cancellationToken).ConfigureAwait(false))
                .ToDictionary(x => itemTokenization.KeyReader(x));

            var missingIds = filteredResults.Where(x => !objectResults.ContainsKey(x.searchResult.Key)).ToList();
            return missingIds.Count > 0
                ? throw new LiftiException(ExceptionMessages.NotAllRequestedItemsReturned, string.Join(",", missingIds))
                : await this.CreateMatchPhrasesAsync(
                    (x, ct) => new ValueTask<TObject>(objectResults[x]),
                    filteredResults,
                    cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public Task<IEnumerable<DocumentPhrases<TKey, TObject>>> CreateMatchPhrasesAsync<TObject>(
            Func<IReadOnlyList<TKey>, ValueTask<IReadOnlyList<TObject>>> loadItemsAsync,
            CancellationToken cancellationToken = default)
        {
            return this.CreateMatchPhrasesAsync((key, ct) => loadItemsAsync(key), cancellationToken);
        }

        /// <inheritdoc />
        public async Task<IEnumerable<DocumentPhrases<TKey, TObject>>> CreateMatchPhrasesAsync<TObject>(
            Func<TKey, TObject> loadItem,
            CancellationToken cancellationToken = default)
        {
            return await this.CreateMatchPhrasesAsync(
                (keys, ct) => new ValueTask<TObject>(loadItem(keys)),
                cancellationToken)
                .ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<IEnumerable<DocumentPhrases<TKey, TObject>>> CreateMatchPhrasesAsync<TObject>(
            Func<TKey, CancellationToken, ValueTask<TObject>> loadItemAsync,
            CancellationToken cancellationToken = default)
        {
            var itemResults = this.FilterFieldMatches<TObject>();

            return await this.CreateMatchPhrasesAsync(
                loadItemAsync,
                itemResults,
                cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public Task<IEnumerable<DocumentPhrases<TKey, TObject>>> CreateMatchPhrasesAsync<TObject>(
            Func<TKey, ValueTask<TObject>> loadItemAsync,
            CancellationToken cancellationToken = default)
        {
            return this.CreateMatchPhrasesAsync((key, ct) => loadItemAsync(key), cancellationToken);
        }

        /// <inheritdoc />
        public IEnumerable<DocumentPhrases<TKey>> CreateMatchPhrases(Func<TKey, string> loadText)
        {
            return this.CreateMatchPhrasesAsync((key, ct) => new ValueTask<ReadOnlyMemory<char>>(loadText(key).AsMemory()))
                .GetAwaiter().GetResult();
        }

        /// <inheritdoc />
        public IEnumerable<DocumentPhrases<TKey>> CreateMatchPhrases(Func<TKey, ReadOnlyMemory<char>> loadText)
        {
            return this.CreateMatchPhrasesAsync((key, ct) => new ValueTask<ReadOnlyMemory<char>>(loadText(key)))
                .GetAwaiter().GetResult();
        }

        /// <inheritdoc />
        public async Task<IEnumerable<DocumentPhrases<TKey>>> CreateMatchPhrasesAsync(
            Func<TKey, CancellationToken, ValueTask<string>> loadTextAsync,
            CancellationToken cancellationToken = default)
        {
            var objectTokenization = this.index.DefaultTokenizer;
            var objectResults = this.FilterFieldMatches(field => field == IndexedFieldLookup.DefaultFieldName);

            return await this.CreateMatchPhrasesAsync(
                async (key, ct) => (await loadTextAsync(key, ct).ConfigureAwait(false)).AsMemory(),
                objectResults,
                cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public Task<IEnumerable<DocumentPhrases<TKey>>> CreateMatchPhrasesAsync(
            Func<TKey, ValueTask<string>> loadTextAsync,
            CancellationToken cancellationToken = default)
        {
            return this.CreateMatchPhrasesAsync(
                async (key, ct) => (await loadTextAsync(key).ConfigureAwait(false)).AsMemory(),
                cancellationToken);
        }

        /// <inheritdoc />
        public async Task<IEnumerable<DocumentPhrases<TKey>>> CreateMatchPhrasesAsync(
            Func<TKey, CancellationToken, ValueTask<ReadOnlyMemory<char>>> loadTextAsync,
            CancellationToken cancellationToken = default)
        {
            var objectTokenization = this.index.DefaultTokenizer;
            var objectResults = this.FilterFieldMatches(field => field == IndexedFieldLookup.DefaultFieldName);

            return await this.CreateMatchPhrasesAsync(loadTextAsync, objectResults, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public Task<IEnumerable<DocumentPhrases<TKey>>> CreateMatchPhrasesAsync(
            Func<TKey, ValueTask<ReadOnlyMemory<char>>> loadTextAsync,
            CancellationToken cancellationToken = default)
        {
            return this.CreateMatchPhrasesAsync(
                (key, ct) => loadTextAsync(key),
                cancellationToken);
        }

        private async Task<IEnumerable<DocumentPhrases<TKey>>> CreateMatchPhrasesAsync(
            Func<TKey, CancellationToken, ValueTask<ReadOnlyMemory<char>>> loadTextAsync,
            List<(SearchResult<TKey> searchResult, List<FieldSearchResult> fieldMatches)> objectResults,
            CancellationToken cancellationToken)
        {
            var phraseBuilder = new StringBuilder();
            var matchedPhrases = new List<DocumentPhrases<TKey>>(this.Results.Count);

            // Create an array that can be used on each call to VirtualString
            var textArray = new ReadOnlyMemory<char>[1];
            foreach (var (searchResult, fieldMatches) in objectResults)
            {
                textArray[0] = await loadTextAsync(searchResult.Key, cancellationToken).ConfigureAwait(false);
                var text = new VirtualString(textArray);

                var fieldPhrases = new List<FieldPhrases<TKey>>(fieldMatches.Count);
                foreach (var fieldMatch in fieldMatches)
                {
                    fieldPhrases.Add(CreatePhrases(fieldMatch, text, phraseBuilder));
                }

                matchedPhrases.Add(new DocumentPhrases<TKey>(searchResult, fieldPhrases));
            }

            return matchedPhrases;
        }

        private async Task<IEnumerable<DocumentPhrases<TKey, TObject>>> CreateMatchPhrasesAsync<TObject>(
            Func<TKey, CancellationToken, ValueTask<TObject>> loadItemAsync,
            List<(SearchResult<TKey> searchResult, List<FieldSearchResult> fieldMatches)> objectResults,
            CancellationToken cancellationToken)
        {
            var phraseBuilder = new StringBuilder();
            var matchedPhrases = new List<DocumentPhrases<TKey, TObject>>(this.Results.Count);
            foreach (var (searchResult, fieldMatches) in objectResults)
            {
                var item = await loadItemAsync(searchResult.Key, cancellationToken).ConfigureAwait(false);
                if (item == null)
                {
                    throw new LiftiException(ExceptionMessages.NoItemReturnedWhenGeneratingMatchPhrases, searchResult.Key);
                }

                var fieldPhrases = new List<FieldPhrases<TKey>>(fieldMatches.Count);
                foreach (var fieldMatch in fieldMatches)
                {
                    var fieldInfo = this.index.FieldLookup.GetFieldInfo(fieldMatch.FoundIn);
                    var text = new VirtualString(await fieldInfo.ReadAsync(item, cancellationToken).ConfigureAwait(false));
                    fieldPhrases.Add(CreatePhrases(fieldMatch, text, phraseBuilder));
                }

                matchedPhrases.Add(new DocumentPhrases<TKey, TObject>(item, searchResult, fieldPhrases));
            }

            return matchedPhrases;
        }

        private List<(SearchResult<TKey> searchResult, List<FieldSearchResult> fieldMatches)> FilterFieldMatches<TObject>()
        {
            return this.FilterFieldMatches(fieldName => this.index.FieldLookup.IsKnownField(typeof(TObject), fieldName));
        }

        private List<(SearchResult<TKey> searchResult, List<FieldSearchResult> fieldMatches)> FilterFieldMatches(
            Func<string, bool> useFieldForMatch)
        {
            // Technically an index can contain fields from multiple object types, so not all results may
            // be appropriate for the requested object type.
            return this.Results
                .Select(x =>
                    (
                        SearchResult: x,
                        FieldMatches: x.FieldMatches
                            .Where(match => useFieldForMatch(match.FoundIn))
                            .ToList())
                    )
                .Where(x => x.FieldMatches.Count > 0)
                .ToList();
        }

        private static FieldPhrases<TKey> CreatePhrases(FieldSearchResult fieldMatch, VirtualString text, StringBuilder phraseBuilder)
        {
            var phrases = new List<(int wordCount, string phrase)>();
            var matchLocations = fieldMatch.Locations;
            if (matchLocations.Count == 0)
            {
                // This shouldn't really happen - we should always match a field at one location
                return new FieldPhrases<TKey>(fieldMatch.FoundIn, []);
            }

            var startLocation = matchLocations[0];
            phraseBuilder.Length = 0;
            phraseBuilder.Append(text.Substring(startLocation.Start, startLocation.Length));
            for (var i = 1; i <= matchLocations.Count; i++)
            {
                var lastWordProcessed = i == matchLocations.Count;

                var previousLocation = matchLocations[i - 1];
                if (lastWordProcessed || matchLocations[i].TokenIndex != previousLocation.TokenIndex + 1)
                {
                    // Word is not part of the previous phrase, or we've processed all the matched words.
                    // Emit the previous phrase now.
                    var startOffset = startLocation.Start;
                    var totalLength = previousLocation.Start + previousLocation.Length - startOffset;
                    phrases.Add(
                        (
                            previousLocation.TokenIndex - startLocation.TokenIndex + 1,
                            phraseBuilder.ToString()
                        ));

                    if (!lastWordProcessed)
                    {
                        // This word starts the next phrase
                        startLocation = matchLocations[i];
                        phraseBuilder.Length = 0;
                        phraseBuilder.Append(text.Substring(startLocation.Start, startLocation.Length));
                    }
                }
                else
                {
                    // Keep building the current phrase text
                    var currentLocation = matchLocations[i];

                    if (phraseBuilder.Length > 0)
                    {
                        phraseBuilder.Append(' ');
                    }

                    // This word starts the next phrase
                    phraseBuilder.Append(text.Substring(currentLocation.Start, currentLocation.Length));
                }
            }

            return new FieldPhrases<TKey>(
                fieldMatch.FoundIn,
                phrases.GroupBy(x => x.phrase, StringComparer.InvariantCultureIgnoreCase)
                    // Sort by the phrases with the most amount of words
                    .OrderByDescending(x => x.Max(g => g.wordCount))
                    // Breaking ties with the most frequently occurring
                    .ThenByDescending(x => x.Count())
                    .Select(f => f.Key)
                    .ToList());
        }

        /// <inheritdoc />
        public IEnumerator<SearchResult<TKey>> GetEnumerator()
        {
            return this.Results.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}

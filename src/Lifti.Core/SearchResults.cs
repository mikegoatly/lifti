using Lifti.Tokenization.Objects;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace Lifti
{
    /// <summary>
    /// Contains search results returned from an index, with additional operations that can be performed, 
    /// e.g. re-ordering them and extracting search phrases from the original source text.
    /// </summary>
    internal class SearchResults<TKey> : ISearchResults<TKey>
        where TKey : notnull
    {
        private readonly IReadOnlyCollection<SearchResult<TKey>> searchResults;
        private readonly FullTextIndex<TKey> index;

        internal SearchResults(FullTextIndex<TKey> index, IEnumerable<SearchResult<TKey>> searchResults)
        {
            this.searchResults = searchResults as IReadOnlyCollection<SearchResult<TKey>> ?? searchResults.ToList();
            this.index = index;
        }

        /// <inheritdoc />
        public ISearchResults<TKey> OrderByField(string fieldName)
        {
            return new SearchResults<TKey>(
                this.index,
                this.searchResults.OrderByDescending(r => r.FieldMatches.Sum(f => f.FoundIn == fieldName ? f.Score : 0D)));
        }

        /// <inheritdoc />
        public async Task<IEnumerable<ItemPhrases<TKey, TItem>>> CreateMatchPhrasesAsync<TItem>(
            Func<IReadOnlyList<TKey>, IReadOnlyList<TItem>> loadItems,
            CancellationToken cancellationToken = default)
        {
            return await this.CreateMatchPhrasesAsync(
                (keys, ct) => new ValueTask<IReadOnlyList<TItem>>(loadItems(keys)),
                cancellationToken)
                .ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<IEnumerable<ItemPhrases<TKey, TItem>>> CreateMatchPhrasesAsync<TItem>(
            Func<IReadOnlyList<TKey>, CancellationToken, ValueTask<IReadOnlyList<TItem>>> loadItemsAsync,
            CancellationToken cancellationToken = default)
        {
            if (loadItemsAsync is null)
            {
                throw new ArgumentNullException(nameof(loadItemsAsync));
            }

            var itemTokenization = this.index.ItemTokenizationOptions.Get<TItem>();
            var itemResults = this.FilterFieldMatches(itemTokenization.FieldReaders.ContainsKey);

            var items = (await loadItemsAsync(itemResults.Select(x => x.searchResult.Key).ToList(), cancellationToken).ConfigureAwait(false))
                .ToDictionary(x => itemTokenization.KeyReader(x));

            var missingIds = itemResults.Where(x => !items.ContainsKey(x.searchResult.Key)).ToList();
            return missingIds.Count > 0
                ? throw new LiftiException(ExceptionMessages.NotAllRequestedItemsReturned, string.Join(",", missingIds))
                : await this.CreateMatchPhrasesAsync(
                (x, ct) => new ValueTask<TItem>(items[x]),
                itemTokenization,
                itemResults,
                cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public Task<IEnumerable<ItemPhrases<TKey, TItem>>> CreateMatchPhrasesAsync<TItem>(
            Func<IReadOnlyList<TKey>, ValueTask<IReadOnlyList<TItem>>> loadItemsAsync,
            CancellationToken cancellationToken = default)
        {
            return this.CreateMatchPhrasesAsync((key, ct) => loadItemsAsync(key), cancellationToken);
        }

        /// <inheritdoc />
        public async Task<IEnumerable<ItemPhrases<TKey, TItem>>> CreateMatchPhrasesAsync<TItem>(
        Func<TKey, TItem> loadItem,
        CancellationToken cancellationToken = default)
        {
            return await this.CreateMatchPhrasesAsync(
                (keys, ct) => new ValueTask<TItem>(loadItem(keys)),
                cancellationToken)
                .ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<IEnumerable<ItemPhrases<TKey, TItem>>> CreateMatchPhrasesAsync<TItem>(
            Func<TKey, CancellationToken, ValueTask<TItem>> loadItemAsync,
            CancellationToken cancellationToken = default)
        {
            var itemTokenization = this.index.ItemTokenizationOptions.Get<TItem>();
            var itemResults = this.FilterFieldMatches(itemTokenization.FieldReaders.ContainsKey);

            return await this.CreateMatchPhrasesAsync(
                loadItemAsync,
                itemTokenization,
                itemResults,
                cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public Task<IEnumerable<ItemPhrases<TKey, TItem>>> CreateMatchPhrasesAsync<TItem>(
            Func<TKey, ValueTask<TItem>> loadItemAsync,
            CancellationToken cancellationToken = default)
        {
            return this.CreateMatchPhrasesAsync((key, ct) => loadItemAsync(key), cancellationToken);
        }

        /// <inheritdoc />
        public IEnumerable<ItemPhrases<TKey>> CreateMatchPhrases(Func<TKey, string> loadText)
        {
            return this.CreateMatchPhrasesAsync((key, ct) => new ValueTask<string>(loadText(key)))
                .GetAwaiter().GetResult();
        }

        /// <inheritdoc />
        public async Task<IEnumerable<ItemPhrases<TKey>>> CreateMatchPhrasesAsync(
            Func<TKey, CancellationToken, ValueTask<string>> loadTextAsync,
            CancellationToken cancellationToken = default)
        {
            var itemTokenization = this.index.DefaultTokenizer;
            var itemResults = this.FilterFieldMatches(field => field == IndexedFieldLookup.DefaultFieldName);

            return await this.CreateMatchPhrasesAsync(loadTextAsync, itemResults, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public Task<IEnumerable<ItemPhrases<TKey>>> CreateMatchPhrasesAsync(
            Func<TKey, ValueTask<string>> loadTextAsync,
            CancellationToken cancellationToken = default)
        {
            return this.CreateMatchPhrasesAsync(
                (key, ct) => loadTextAsync(key),
                cancellationToken);
        }

        private async Task<IEnumerable<ItemPhrases<TKey>>> CreateMatchPhrasesAsync(
            Func<TKey, CancellationToken, ValueTask<string>> loadTextAsync,
            List<(SearchResult<TKey> searchResult, List<FieldSearchResult> fieldMatches)> itemResults,
            CancellationToken cancellationToken)
        {
            var matchedPhrases = new List<ItemPhrases<TKey>>(this.searchResults.Count);

            // Create an array that can be used on each call to VirtualString
            var textArray = new string[1];
            foreach (var (searchResult, fieldMatches) in itemResults)
            {
                textArray[0] = await loadTextAsync(searchResult.Key, cancellationToken).ConfigureAwait(false);
                var text = new VirtualString(textArray);

                var fieldPhrases = new List<FieldPhrases<TKey>>(fieldMatches.Count);
                foreach (var fieldMatch in fieldMatches)
                {
                    fieldPhrases.Add(CreatePhrases(fieldMatch, text));
                }

                matchedPhrases.Add(new ItemPhrases<TKey>(searchResult, fieldPhrases));
            }

            return matchedPhrases;
        }

        private async Task<IEnumerable<ItemPhrases<TKey, TItem>>> CreateMatchPhrasesAsync<TItem>(
            Func<TKey, CancellationToken, ValueTask<TItem>> loadItemAsync,
            ObjectTokenization<TItem, TKey> itemTokenization,
            List<(SearchResult<TKey> searchResult, List<FieldSearchResult> fieldMatches)> itemResults,
            CancellationToken cancellationToken)
        {
            var matchedPhrases = new List<ItemPhrases<TKey, TItem>>(this.searchResults.Count);
            foreach (var (searchResult, fieldMatches) in itemResults)
            {
                var item = await loadItemAsync(searchResult.Key, cancellationToken).ConfigureAwait(false);

                var fieldPhrases = new List<FieldPhrases<TKey>>(fieldMatches.Count);
                foreach (var fieldMatch in fieldMatches)
                {
                    if (itemTokenization.FieldReaders.TryGetValue(fieldMatch.FoundIn, out var fieldReader))
                    {
                        var text = new VirtualString(await fieldReader.ReadAsync(item, cancellationToken).ConfigureAwait(false));
                        fieldPhrases.Add(CreatePhrases(fieldMatch, text));
                    }
                }

                matchedPhrases.Add(new ItemPhrases<TKey, TItem>(item, searchResult, fieldPhrases));
            }

            return matchedPhrases;
        }

        private List<(SearchResult<TKey> searchResult, List<FieldSearchResult> fieldMatches)> FilterFieldMatches(
            Func<string, bool> useFieldForMatch)
        {
            // Technically an index can contain fields from multiple item sources, so not all results may
            // be appropriate for the requested item type.
            return this.searchResults
                .Select(x =>
                    (
                        ItemKey: x,
                        FieldMatches: x.FieldMatches
                            .Where(match => useFieldForMatch(match.FoundIn))
                            .ToList())
                    )
                .Where(x => x.FieldMatches.Count > 0)
                .ToList();
        }

        private static FieldPhrases<TKey> CreatePhrases(FieldSearchResult fieldMatch, VirtualString text)
        {
            var phrases = new List<(int wordCount, string phrase)>();
            var matchLocations = fieldMatch.Locations;
            if (matchLocations.Count == 0)
            {
                // This shouldn't really happen - we should always match a field at one location
                return new FieldPhrases<TKey>(fieldMatch.FoundIn, Array.Empty<string>());
            }

            var startLocation = matchLocations[0];
            for (var i = 1; i <= matchLocations.Count; i++)
            {
                var previousLocation = matchLocations[i - 1];
                if (i == matchLocations.Count || matchLocations[i].TokenIndex != previousLocation.TokenIndex + 1)
                {
                    // Word is not part of the previous phrase, or we've processed all the matched words.
                    // Emit the previous phrase now.
                    var startOffset = startLocation.Start;
                    var totalLength = previousLocation.Start + previousLocation.Length - startOffset;
                    phrases.Add(
                        (
                            previousLocation.TokenIndex - startLocation.TokenIndex + 1,
                            text.Substring(startOffset, totalLength)
                        ));

                    if (i < matchLocations.Count)
                    {
                        // This word starts the next phrase
                        startLocation = matchLocations[i];
                    }
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
            return this.searchResults.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}

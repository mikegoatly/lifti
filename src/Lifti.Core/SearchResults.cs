using Lifti.Tokenization.Objects;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lifti
{
    /// <summary>
    /// Contains search results returned from an index, with additional operations that can be performed, 
    /// e.g. re-ordering them and extracting search phrases from the original source text.
    /// </summary>
    internal class SearchResults<TKey> : ISearchResults<TKey>
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
        public async Task<IEnumerable<MatchedPhrases<TKey, TItem>>> CreateMatchPhrasesAsync<TItem>(
            Func<IReadOnlyList<TKey>, IReadOnlyList<TItem>> loadItems)
        {
            return await this.CreateMatchPhrasesAsync(keys => new ValueTask<IReadOnlyList<TItem>>(loadItems(keys)))
                .ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<IEnumerable<MatchedPhrases<TKey, TItem>>> CreateMatchPhrasesAsync<TItem>(
            Func<IReadOnlyList<TKey>, ValueTask<IReadOnlyList<TItem>>> loadItemsAsync)
        {
            if (loadItemsAsync is null)
            {
                throw new ArgumentNullException(nameof(loadItemsAsync));
            }

            var itemTokenization = this.index.ItemTokenizationOptions.Get<TItem>();
            var itemResults = this.FilterFieldMatches(itemTokenization.FieldReaders.ContainsKey);

            var items = (await loadItemsAsync(itemResults.Select(x => x.ItemKey).ToList()).ConfigureAwait(false))
                .ToDictionary(x => itemTokenization.KeyReader(x));

            var missingIds = itemResults.Where(x => !items.ContainsKey(x.ItemKey)).ToList();
            return missingIds.Count > 0
                ? throw new LiftiException(ExceptionMessages.NotAllRequestedItemsReturned, string.Join(",", missingIds))
                : await this.CreateMatchPhrasesAsync(
                x => new ValueTask<TItem>(items[x]),
                itemTokenization,
                itemResults).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<IEnumerable<MatchedPhrases<TKey, TItem>>> CreateMatchPhrasesAsync<TItem>(
            Func<TKey, TItem> loadItem)
        {
            return await this.CreateMatchPhrasesAsync(keys => new ValueTask<TItem>(loadItem(keys)))
                .ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<IEnumerable<MatchedPhrases<TKey, TItem>>> CreateMatchPhrasesAsync<TItem>(
            Func<TKey, ValueTask<TItem>> loadItemAsync)
        {
            var itemTokenization = this.index.ItemTokenizationOptions.Get<TItem>();
            var itemResults = this.FilterFieldMatches(itemTokenization.FieldReaders.ContainsKey);

            return await this.CreateMatchPhrasesAsync(loadItemAsync, itemTokenization, itemResults).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<IEnumerable<MatchedPhrases<TKey>>> CreateMatchPhrasesAsync(
            Func<TKey, string> loadText)
        {
            return await this.CreateMatchPhrasesAsync(key => new ValueTask<string>(loadText(key))).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<IEnumerable<MatchedPhrases<TKey>>> CreateMatchPhrasesAsync(
            Func<TKey, ValueTask<string>> loadTextAsync)
        {
            var itemTokenization = this.index.DefaultTokenizer;
            var itemResults = this.FilterFieldMatches(field => field == IndexedFieldLookup.DefaultFieldName);

            return await this.CreateMatchPhrasesAsync(loadTextAsync, itemResults).ConfigureAwait(false);
        }

        private async Task<IEnumerable<MatchedPhrases<TKey>>> CreateMatchPhrasesAsync(
            Func<TKey, ValueTask<string>> loadTextAsync,
            List<(TKey ItemKey, List<FieldSearchResult> FieldMatches)> itemResults)
        {
            var matchedPhrases = new List<MatchedPhrases<TKey, TKey>>(this.searchResults.Count);

            // Create an array that can be used on each call to VirtualString
            var textArray = new string[1];
            foreach (var (ItemKey, FieldMatches) in itemResults)
            {
                textArray[0] = await loadTextAsync(ItemKey).ConfigureAwait(false);
                var text = new VirtualString(textArray);

                foreach (var fieldMatch in FieldMatches)
                {
                    var itemPhrases = CreatePhrases(ItemKey, ItemKey, fieldMatch, text);
                    matchedPhrases.Add(itemPhrases);
                }
            }

            return matchedPhrases;
        }

        private async Task<IEnumerable<MatchedPhrases<TKey, TItem>>> CreateMatchPhrasesAsync<TItem>(
            Func<TKey, ValueTask<TItem>> loadItemAsync,
            ObjectTokenization<TItem, TKey> itemTokenization,
            List<(TKey ItemKey, List<FieldSearchResult> FieldMatches)> itemResults)
        {
            var matchedPhrases = new List<MatchedPhrases<TKey, TItem>>(this.searchResults.Count);
            foreach (var (ItemKey, FieldMatches) in itemResults)
            {
                var item = await loadItemAsync(ItemKey).ConfigureAwait(false);

                foreach (var fieldMatch in FieldMatches)
                {
                    if (itemTokenization.FieldReaders.TryGetValue(fieldMatch.FoundIn, out var fieldReader))
                    {
                        var text = new VirtualString(await fieldReader.ReadAsync(item).ConfigureAwait(false));
                        var itemPhrases = CreatePhrases(ItemKey, item, fieldMatch, text);
                        matchedPhrases.Add(itemPhrases);
                    }
                }
            }

            return matchedPhrases;
        }

        private List<(TKey ItemKey, List<FieldSearchResult> FieldMatches)> FilterFieldMatches(
            Func<string, bool> useFieldForMatch)
        {
            // Technically an index can contain fields from multiple item sources, so not all results may
            // be appropriate for the requested item type.
            return this.searchResults
                .Select(x =>
                    (
                        ItemKey: x.Key,
                        FieldMatches: x.FieldMatches
                            .Where(match => useFieldForMatch(match.FoundIn))
                            .ToList())
                    )
                .Where(x => x.FieldMatches.Count > 0)
                .ToList();
        }

        private static MatchedPhrases<TKey, TItem> CreatePhrases<TItem>(TKey key, TItem item, FieldSearchResult fieldMatch, VirtualString text)
        {
            var phrases = new List<(int wordCount, string phrase)>();
            var matchLocations = fieldMatch.Locations;
            if (matchLocations.Count == 0)
            {
                // This shouldn't really happen - we should always match a field at one location
                return new MatchedPhrases<TKey, TItem>(item, key, Array.Empty<string>());
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

            return new MatchedPhrases<TKey, TItem>(
                item,
                key,
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

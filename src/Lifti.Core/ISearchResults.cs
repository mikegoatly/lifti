using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Lifti
{
    /// <summary>
    /// Describes a set of search results from an index.
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    public interface ISearchResults<TKey> : IEnumerable<SearchResult<TKey>>
    {
        /// <inheritdoc cref="CreateMatchPhrasesAsync{TItem}(Func{TKey, TItem}, CancellationToken)"/>
        /// <param name="loadItems">
        /// A function capable of retrieving all the original items that were indexed.
        /// The loaded text will be assumed to be unchanged since the item was indexed and <bold>all</bold> items must be returned,
        /// though the order is unimportant. An exception will be thrown if requested items are missing from the returned list.
        /// </param>
        /// <param name="cancellationToken">
        /// The optional <see cref="CancellationToken"/> to use.
        /// </param>
        Task<IEnumerable<MatchedPhrases<TKey, TItem>>> CreateMatchPhrasesAsync<TItem>(
            Func<IReadOnlyList<TKey>, IReadOnlyList<TItem>> loadItems,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates phrases extracted from the source item's text. Where words appear in sequence they will be combined as a single string. 
        /// Each result has its phrases ordered by the those with most words, then by the most frequent phrases.
        /// </summary>
        /// <param name="loadItem">
        /// A function capable of retrieving the original item that was indexed. The loaded text will be assumed to be unchanged since the item was indexed.
        /// </param>
        /// <param name="cancellationToken">
        /// The optional <see cref="CancellationToken"/> to use.
        /// </param>
        Task<IEnumerable<MatchedPhrases<TKey, TItem>>> CreateMatchPhrasesAsync<TItem>(
            Func<TKey, TItem> loadItem,
            CancellationToken cancellationToken = default);


        /// <inheritdoc cref="CreateMatchPhrasesAsync{TItem}(Func{IReadOnlyList{TKey}, IReadOnlyList{TItem}}, CancellationToken)"/>
        /// <param name="loadItemsAsync">
        /// A function capable of retrieving all the original items that were indexed.
        /// The loaded text will be assumed to be unchanged since the item was indexed and <bold>all</bold> items must be returned,
        /// though the order is unimportant. An exception will be thrown if requested items are missing from the returned list.
        /// </param>
        /// <param name="cancellationToken">
        /// The optional <see cref="CancellationToken"/> to use.
        /// </param>
        Task<IEnumerable<MatchedPhrases<TKey, TItem>>> CreateMatchPhrasesAsync<TItem>(
            Func<IReadOnlyList<TKey>, CancellationToken, ValueTask<IReadOnlyList<TItem>>> loadItemsAsync,
            CancellationToken cancellationToken = default);

        /// <inheritdoc cref="CreateMatchPhrasesAsync{TItem}(Func{IReadOnlyList{TKey}, CancellationToken, ValueTask{IReadOnlyList{TItem}}}, CancellationToken)"/>
        Task<IEnumerable<MatchedPhrases<TKey, TItem>>> CreateMatchPhrasesAsync<TItem>(
            Func<IReadOnlyList<TKey>, ValueTask<IReadOnlyList<TItem>>> loadItemsAsync,
            CancellationToken cancellationToken = default);

        /// <inheritdoc cref="CreateMatchPhrasesAsync{TItem}(Func{TKey, TItem}, CancellationToken)"/>
        /// <param name="loadItemAsync">
        /// A function capable of asynchronously retrieving the original item that was indexed. The loaded text will be assumed to be unchanged since the item was indexed.
        /// </param>
        /// <param name="cancellationToken">
        /// The optional <see cref="CancellationToken"/> to use.
        /// </param>
        Task<IEnumerable<MatchedPhrases<TKey, TItem>>> CreateMatchPhrasesAsync<TItem>(
            Func<TKey, CancellationToken, ValueTask<TItem>> loadItemAsync,
            CancellationToken cancellationToken = default);

        /// <inheritdoc cref="CreateMatchPhrasesAsync(Func{TKey, CancellationToken, ValueTask{string}}, CancellationToken)"/>
        Task<IEnumerable<MatchedPhrases<TKey, TItem>>> CreateMatchPhrasesAsync<TItem>(
             Func<TKey, ValueTask<TItem>> loadItemAsync,
             CancellationToken cancellationToken = default);

        /// <inheritdoc cref="CreateMatchPhrasesAsync{TItem}(Func{TKey, TItem}, CancellationToken)"/>
        /// <param name="loadText">A function capable of retrieving the original text that was indexed against the key. The loaded text will be assumed to be 
        /// unchanged since it was indexed.</param>
        IEnumerable<MatchedPhrases<TKey>> CreateMatchPhrases(
            Func<TKey, string> loadText);

        /// <inheritdoc cref="CreateMatchPhrasesAsync{TItem}(Func{TKey, TItem}, CancellationToken)"/>
        /// <param name="loadTextAsync">A function capable of asynchronously retrieving the original text that was indexed against the key. The loaded text will be 
        /// assumed to be unchanged since it was indexed.</param>
        /// <param name="cancellationToken">
        /// The optional <see cref="CancellationToken"/> to use.
        /// </param>
        Task<IEnumerable<MatchedPhrases<TKey>>> CreateMatchPhrasesAsync(
            Func<TKey, CancellationToken, ValueTask<string>> loadTextAsync,
            CancellationToken cancellationToken = default);

        /// <inheritdoc cref="CreateMatchPhrasesAsync(Func{TKey, CancellationToken, ValueTask{string}}, CancellationToken)"/>
        Task<IEnumerable<MatchedPhrases<TKey>>> CreateMatchPhrasesAsync(
            Func<TKey, ValueTask<string>> loadTextAsync,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns a new instance with the search results re-ordered, only considering scores for a single field.
        /// </summary>
        /// <param name="fieldName">The name of the field to order the search results by.</param>
        ISearchResults<TKey> OrderByField(string fieldName);
    }
}
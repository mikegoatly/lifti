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
        /// <summary>
        /// Gets the number of results in the set.
        /// </summary>
        int Count { get; }

        /// <inheritdoc cref="CreateMatchPhrasesAsync{TObject}(Func{TKey, TObject}, CancellationToken)"/>
        /// <param name="loadItems">
        /// A function capable of retrieving all the original objects that were indexed.
        /// The loaded text will be assumed to be unchanged since the object was indexed and <bold>all</bold> items must be returned,
        /// though the order is unimportant. An exception will be thrown if requested objects are missing from the returned list.
        /// </param>
        /// <param name="cancellationToken">
        /// The optional <see cref="CancellationToken"/> to use.
        /// </param>
        /// <exception cref="LiftiException">
        /// Thrown if the <paramref name="loadItems"/> function does not return all the requested objects.
        /// </exception>
        Task<IEnumerable<DocumentPhrases<TKey, TObject>>> CreateMatchPhrasesAsync<TObject>(
            Func<IReadOnlyList<TKey>, IReadOnlyList<TObject>> loadItems,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates phrases extracted from the source object's text. Where words appear in sequence they will be combined as a single string. 
        /// Each result has its phrases ordered by the those with most words, then by the most frequent phrases.
        /// </summary>
        /// <param name="loadItem">
        /// A function capable of retrieving the original object that was indexed. The loaded text will be assumed to be unchanged since the object was indexed.
        /// </param>
        /// <param name="cancellationToken">
        /// The optional <see cref="CancellationToken"/> to use.
        /// </param>
        Task<IEnumerable<DocumentPhrases<TKey, TObject>>> CreateMatchPhrasesAsync<TObject>(
            Func<TKey, TObject> loadItem,
            CancellationToken cancellationToken = default);


        /// <inheritdoc cref="CreateMatchPhrasesAsync{TObject}(Func{IReadOnlyList{TKey}, IReadOnlyList{TObject}}, CancellationToken)"/>
        /// <param name="loadItemsAsync">
        /// A function capable of retrieving all the original items that were indexed.
        /// The loaded text will be assumed to be unchanged since the object was indexed and <bold>all</bold> objects must be returned,
        /// though the order is unimportant. An exception will be thrown if requested objects are missing from the returned list.
        /// </param>
        /// <param name="cancellationToken">
        /// The optional <see cref="CancellationToken"/> to use.
        /// </param>
        /// <exception cref="LiftiException">
        /// Thrown if the <paramref name="loadItemsAsync"/> function does not return all the requested objects.
        /// </exception>
        Task<IEnumerable<DocumentPhrases<TKey, TObject>>> CreateMatchPhrasesAsync<TObject>(
            Func<IReadOnlyList<TKey>, CancellationToken, ValueTask<IReadOnlyList<TObject>>> loadItemsAsync,
            CancellationToken cancellationToken = default);

        /// <inheritdoc cref="CreateMatchPhrasesAsync{TObject}(Func{IReadOnlyList{TKey}, CancellationToken, ValueTask{IReadOnlyList{TObject}}}, CancellationToken)"/>
        Task<IEnumerable<DocumentPhrases<TKey, TObject>>> CreateMatchPhrasesAsync<TObject>(
            Func<IReadOnlyList<TKey>, ValueTask<IReadOnlyList<TObject>>> loadItemsAsync,
            CancellationToken cancellationToken = default);

        /// <inheritdoc cref="CreateMatchPhrasesAsync{TObject}(Func{TKey, TObject}, CancellationToken)"/>
        /// <param name="loadItemAsync">
        /// A function capable of asynchronously retrieving the original item that was indexed. The loaded text will be assumed to be unchanged since the object was indexed.
        /// </param>
        /// <param name="cancellationToken">
        /// The optional <see cref="CancellationToken"/> to use.
        /// </param>
        Task<IEnumerable<DocumentPhrases<TKey, TObject>>> CreateMatchPhrasesAsync<TObject>(
            Func<TKey, CancellationToken, ValueTask<TObject>> loadItemAsync,
            CancellationToken cancellationToken = default);

        /// <inheritdoc cref="CreateMatchPhrasesAsync(Func{TKey, CancellationToken, ValueTask{string}}, CancellationToken)"/>
        Task<IEnumerable<DocumentPhrases<TKey, TObject>>> CreateMatchPhrasesAsync<TObject>(
             Func<TKey, ValueTask<TObject>> loadItemAsync,
             CancellationToken cancellationToken = default);

        /// <inheritdoc cref="CreateMatchPhrasesAsync{TObject}(Func{TKey, TObject}, CancellationToken)"/>
        /// <param name="loadText">A function capable of retrieving the original text that was indexed against the key. The loaded text will be assumed to be 
        /// unchanged since it was indexed.</param>
        IEnumerable<DocumentPhrases<TKey>> CreateMatchPhrases(
            Func<TKey, string> loadText);

        /// <inheritdoc cref="CreateMatchPhrasesAsync{TObject}(Func{TKey, TObject}, CancellationToken)"/>
        /// <param name="loadText">A function capable of retrieving the original text that was indexed against the key. The loaded text will be assumed to be 
        /// unchanged since it was indexed.</param>
        IEnumerable<DocumentPhrases<TKey>> CreateMatchPhrases(
            Func<TKey, ReadOnlyMemory<char>> loadText);

        /// <inheritdoc cref="CreateMatchPhrasesAsync{TObject}(Func{TKey, TObject}, CancellationToken)"/>
        /// <param name="loadTextAsync">A function capable of asynchronously retrieving the original text that was indexed against the key. The loaded text will be 
        /// assumed to be unchanged since it was indexed.</param>
        /// <param name="cancellationToken">
        /// The optional <see cref="CancellationToken"/> to use.
        /// </param>
        Task<IEnumerable<DocumentPhrases<TKey>>> CreateMatchPhrasesAsync(
            Func<TKey, CancellationToken, ValueTask<string>> loadTextAsync,
            CancellationToken cancellationToken = default);

        /// <inheritdoc cref="CreateMatchPhrasesAsync{TObject}(Func{TKey, TObject}, CancellationToken)"/>
        /// <param name="loadTextAsync">A function capable of asynchronously retrieving the original text that was indexed against the key. The loaded text will be 
        /// assumed to be unchanged since it was indexed.</param>
        /// <param name="cancellationToken">
        /// The optional <see cref="CancellationToken"/> to use.
        /// </param>
        Task<IEnumerable<DocumentPhrases<TKey>>> CreateMatchPhrasesAsync(
            Func<TKey, CancellationToken, ValueTask<ReadOnlyMemory<char>>> loadTextAsync,
            CancellationToken cancellationToken = default);

        /// <inheritdoc cref="CreateMatchPhrasesAsync(Func{TKey, CancellationToken, ValueTask{string}}, CancellationToken)"/>
        Task<IEnumerable<DocumentPhrases<TKey>>> CreateMatchPhrasesAsync(
            Func<TKey, ValueTask<string>> loadTextAsync,
            CancellationToken cancellationToken = default);


        /// <inheritdoc cref="CreateMatchPhrasesAsync(Func{TKey, CancellationToken, ValueTask{string}}, CancellationToken)"/>
        Task<IEnumerable<DocumentPhrases<TKey>>> CreateMatchPhrasesAsync(
            Func<TKey, ValueTask<ReadOnlyMemory<char>>> loadTextAsync,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns a new instance with the search results re-ordered, only considering scores for a single field.
        /// </summary>
        /// <param name="fieldName">The name of the field to order the search results by.</param>
        ISearchResults<TKey> OrderByField(string fieldName);

        /// <summary>
        /// Builds an actual execution plan for the query.
        /// </summary>
        QueryExecutionPlan GetExecutionPlan();
    }
}
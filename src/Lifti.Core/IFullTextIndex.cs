using Lifti.Querying;
using Lifti.Tokenization;
using Lifti.Tokenization.TextExtraction;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Lifti
{
    /// <summary>
    /// </summary>
    public interface IFullTextIndex<TKey> : IIndexTokenizerProvider
    {
        /// <summary>
        /// Internally an index keeps track of items and their metadata. Can be used get ids for items and 
        /// visa-versa, along with other derived metadata such as token counts.
        /// </summary>
        IItemStore<TKey> Items { get; }

        /// <summary>
        /// Fields are tracked internally as a id of type <see cref="byte"/>. This lookup can
        /// be used to get the field associated to an id, and visa versa.
        /// </summary>
        IIndexedFieldLookup FieldLookup { get; }

        /// <summary>
        /// Gets the number of items contained in the index. This will not reflect any new items
        /// that are currently being inserted in a batch until the batch completes.
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Gets a self-consistent snapshot of the index that can be used to create navigators and query the index
        /// whilst it is being mutated.
        /// </summary>
        IIndexSnapshot<TKey> Snapshot { get; }

        /// <summary>
        /// Gets the configured <see cref="IQueryParser"/> for the index. If you need to execute the same query against the index multiple
        /// times, you can use this to parse a query as an <see cref="IQuery"/>, and then execute that against the index's <see cref="Search(IQuery)"/> method.
        /// </summary>
        IQueryParser QueryParser { get; }

        /// <summary>
        /// Gets the default <see cref="ITextExtractor"/> implementation that the index will use when one is
        /// not explicitly configured for a field.
        /// </summary>
        ITextExtractor DefaultTextExtractor { get; }

        /// <summary>
        /// Gets the default <see cref="IThesaurus"/> implementation that will be used while indexing text when one
        /// is not explicitly configured for a field.
        /// </summary>
        IThesaurus DefaultThesaurus { get; }

        /// <summary>
        /// Uses the current snapshot of the index to create an implementation of <see cref="IIndexNavigator"/> that can be used to 
        /// navigate through the index on a character by character basis. Provided as convenience for use instead of calling
        /// <see cref="IIndexSnapshot.CreateNavigator()" /> on <see cref="IFullTextIndex{T}.Snapshot" /> directly
        /// </summary>
        IIndexNavigator CreateNavigator();

        /// <summary>
        /// Indexes some text against a given key.
        /// </summary>
        /// <param name="itemKey">The key of the item being indexed.</param>
        /// <param name="text">The text to index against the item.</param>
        /// <param name="cancellationToken">The optional <see cref="CancellationToken"/> for the operation.</param>
        Task AddAsync(TKey itemKey, string text, CancellationToken cancellationToken = default);

        /// <summary>
        /// Indexes some text against a given key.
        /// </summary>
        /// <param name="itemKey">The key of the item being indexed.</param>
        /// <param name="text">The text to index against the item.</param>
        /// <param name="cancellationToken">The optional <see cref="CancellationToken"/> for the operation.</param>
        Task AddAsync(TKey itemKey, IEnumerable<string> text, CancellationToken cancellationToken = default);

        /// <summary>
        /// Indexes a single item of type <typeparamref name="TItem"/>. This type must have been
        /// configured when the index was built.
        /// </summary>
        /// <typeparam name="TItem">
        /// The type of the item being indexed.
        /// </typeparam>
        /// <param name="item">
        /// The item to index.
        /// </param>
        /// <param name="cancellationToken">The optional <see cref="CancellationToken"/> for the operation.</param>
        Task AddAsync<TItem>(TItem item, CancellationToken cancellationToken = default);

        /// <summary>
        /// Indexes a set of items of type <typeparamref name="TItem"/>. This type must have been
        /// configured when the index was built.
        /// </summary>
        /// <typeparam name="TItem">
        /// The type of the item being indexed.
        /// </typeparam>
        /// <param name="items">
        /// The items to index.
        /// </param>
        /// <param name="cancellationToken">The optional <see cref="CancellationToken"/> for the operation.</param>
        Task AddRangeAsync<TItem>(IEnumerable<TItem> items, CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes the item with the given key from this index. If the key is not indexed then
        /// this operation is a no-op and <c>false</c> is returned.
        /// </summary>
        /// <param name="itemKey">
        /// The key of the item to remove.
        /// </param>
        /// <returns>
        /// <c>true</c> if the item was in the index, <c>false</c> if it was not.
        /// </returns>
        /// <param name="cancellationToken">The optional <see cref="CancellationToken"/> for the operation.</param>
        Task<bool> RemoveAsync(TKey itemKey, CancellationToken cancellationToken = default);

        /// <summary>
        /// Performs a search against this index.
        /// </summary>
        /// <param name="searchText">
        /// The query to use when searching in the index.
        /// </param>
        /// <returns>
        /// The matching search results.
        /// </returns>
        ISearchResults<TKey> Search(string searchText);

        /// <summary>
        /// Performs a search against this index.
        /// </summary>
        /// <param name="query">
        /// The query to use when searching in the index.
        /// </param>
        /// <returns>
        /// The matching search results.
        /// </returns>
        ISearchResults<TKey> Search(IQuery query);
    }
}
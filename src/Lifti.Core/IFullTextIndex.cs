using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lifti
{
    public interface IFullTextIndex<TKey>
    {
        /// <summary>
        /// Internally an index keeps track of an item as a integer ID reference. This lookup
        /// can be used get ids for items and visa-versa.
        /// </summary>
        IIdLookup<TKey> IdLookup { get; }

        /// <summary>
        /// Fields are tracked internally as a id of type <see cref="byte"/>. This lookup can
        /// be used to get the field associated to an id, and visa versa.
        /// </summary>
        IIndexedFieldLookup FieldLookup { get; }

        /// <summary>
        /// Gets the number of items contained in the index.
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Indexes some text against a given key.
        /// </summary>
        /// <param name="itemKey">The key of the item being indexed.</param>
        /// <param name="text">The text to index against the item.</param>
        /// <param name="tokenizationOptions">
        /// An instance of <see cref="TokenizationOptions"/>
        /// that describes how the text should be treated as it is tokenized.
        /// When null, <see cref="TokenizationOptions.Default"/> will be used.
        /// </param>
        void Add(TKey itemKey, string text, TokenizationOptions tokenizationOptions = null);

        /// <summary>
        /// Indexes some text against a given key.
        /// </summary>
        /// <param name="itemKey">The key of the item being indexed.</param>
        /// <param name="text">The text to index against the item.</param>
        /// <param name="tokenizationOptions">
        /// An instance of <see cref="TokenizationOptions"/>
        /// that describes how the text should be treated as it is tokenized.
        /// When null, <see cref="TokenizationOptions.Default"/> will be used.
        /// </param>
        void Add(TKey itemKey, IEnumerable<string> text, TokenizationOptions tokenizationOptions = null);

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
        void Add<TItem>(TItem item);

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
        void AddRange<TItem>(IEnumerable<TItem> items);

        /// <summary>
        /// Indexes a single item of type <typeparamref name="TItem"/>. This type must have been
        /// configured when the index was built. This async method only needs to be used when an
        /// async accessor for field text has been provided.
        /// </summary>
        /// <typeparam name="TItem">
        /// The type of the item being indexed.
        /// </typeparam>
        /// <param name="item">
        /// The item to index.
        /// </param>
        ValueTask AddAsync<TItem>(TItem item);

        /// <summary>
        /// Indexes a set of items of type <typeparamref name="TItem"/>. This type must have been
        /// configured when the index was built. This async method only needs to be used when an
        /// async accessor for field text has been provided.
        /// </summary>
        /// <typeparam name="TItem">
        /// The type of the item being indexed.
        /// </typeparam>
        /// <param name="items">
        /// The items to index.
        /// </param>
        ValueTask AddRangeAsync<TItem>(IEnumerable<TItem> items);

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
        bool Remove(TKey itemKey);

        /// <summary>
        /// Performs a search against this index.
        /// </summary>
        /// <param name="searchText">
        /// The query to use when searching in the index.
        /// </param>
        /// <param name="tokenizationOptions">
        /// The <see cref="TokenizationOptions"/> to use when tokenizing words in the <paramref name="searchText"/>.
        /// </param>
        /// <returns>
        /// The matching search results.
        /// </returns>
        IEnumerable<SearchResult<TKey>> Search(string searchText, TokenizationOptions tokenizationOptions = null);

        /// <summary>
        /// Creates a self-consistent snapshot of the index that can be used to create navigators and query the index
        /// whilst it is being mutated.
        /// </summary>
        IIndexSnapshot<TKey> Snapshot();
    }
}
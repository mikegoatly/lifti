using Lifti.Querying;
using System;
using System.Collections.Generic;

namespace Lifti
{    
    public interface IFullTextIndex<TKey>
    {
        IIdPool<TKey> IdPool { get; }
        IIndexedFieldLookup FieldLookup { get; }

        /// <summary>
        /// Gets the number of items contained in the index.
        /// </summary>
        int Count { get; }

        IndexNode Root { get; }

        void Index(TKey itemKey, string text, TokenizationOptions tokenizationOptions = null);
        void Index<TItem>(TItem item);

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
        /// Creates an implementation of <see cref="IIndexNavigator"/> that can be used to navigate through the index
        /// on a character by character basis.
        /// </summary>
        IIndexNavigator CreateNavigator();

        /// <summary>
        /// Creates an <see cref="ItemTokenizationOptions{TItem, TKey}"/> configuration entry for an item of type <typeparamref name="TItem"/>
        /// in the index. Subsequent calls to the <see cref="ItemTokenizationOptions{TItem, TKey}.WithField"/> can be used to configure
        /// the fields that should be indexed on the type.
        /// </summary>
        /// <param name="idReader">
        /// A delegate capable of reading the key of type <typeparamref name="TKey"/> to store in the index. The retrieved value must uniquely
        /// identify the object.
        /// </param>
        ItemTokenizationOptions<TItem, TKey> WithItemTokenization<TItem>(Func<TItem, TKey> idReader);
    }
}
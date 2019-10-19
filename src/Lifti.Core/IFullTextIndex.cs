using Lifti.Querying;
using System;
using System.Collections.Generic;

namespace Lifti
{
    public interface IFullTextIndex<TKey>
    {
        IndexNode Root { get; }
        IIdPool<TKey> IdPool { get; }
        IIndexedFieldLookup FieldLookup { get; }
        int Count { get; }

        void Index(TKey itemKey, string text, TokenizationOptions tokenizationOptions = null);
        void Index<TItem>(TItem item);

        bool Remove(TKey itemKey);
        IEnumerable<SearchResult<TKey>> Search(string searchText, TokenizationOptions tokenizationOptions = null);
        IndexNavigator CreateNavigator();
        ItemTokenizationOptions<TItem, TKey> WithItemTokenization<TItem>(Func<TItem, TKey> idReader);
    }
}
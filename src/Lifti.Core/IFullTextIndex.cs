using System.Collections.Generic;

namespace Lifti
{
    public interface IFullTextIndex<TKey>
    {
        IndexNode Root { get; }
        IIdPool<TKey> IdPool { get; }
        IIndexedFieldLookup FieldLookup { get; }

        void Index(TKey itemKey, string text, TokenizationOptions? tokenizationOptions = null);
        void Index<TItem>(TItem item, ItemTokenizationOptions<TItem, TKey> itemTokenizationOptions);
        IEnumerable<SearchResult<TKey>> Search(string searchText, TokenizationOptions? tokenizationOptions = null);
    }
}
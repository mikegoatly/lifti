using System.Collections.Generic;

namespace Lifti.Querying
{
    public interface IQuery
    {
        IEnumerable<SearchResult<TKey>> Execute<TKey>(IFullTextIndex<TKey> index);
    }
}

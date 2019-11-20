using Lifti.Querying.QueryParts;
using System.Collections.Generic;

namespace Lifti.Querying
{
    public interface IQuery
    {
        IQueryPart Root { get; }

        IEnumerable<SearchResult<TKey>> Execute<TKey>(IIndexSnapshot<TKey> index);
    }
}

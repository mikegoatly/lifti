using System.Collections.Generic;

namespace Lifti.Querying
{
    public interface IQueryTokenizer
    {
        IEnumerable<QueryToken> ParseQueryTokens(string queryText);
    }
}
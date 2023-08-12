using Lifti.Querying;
using System.Collections.Generic;

namespace Lifti.Tests.Fakes
{
    public class FakeQueryParser : IQueryParser
    {
        private readonly IQuery returnedQuery;

        public FakeQueryParser(IQuery returnedQuery)
        {
            this.returnedQuery = returnedQuery;
        }

        public List<string> ParsedQueries { get; } = new List<string>();

        public IQuery Parse(IIndexedFieldLookup fieldLookup, string queryText, IIndexTokenizerProvider tokenizerProvider)
        {
            this.ParsedQueries.Add(queryText);

            return this.returnedQuery;
        }
    }
}

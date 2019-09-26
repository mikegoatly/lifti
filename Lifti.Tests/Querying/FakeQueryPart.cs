using Lifti.Querying;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Lifti.Tests.Querying
{
    public class FakeQueryPart : IQueryPart
    {
        private IntermediateQueryResult results;

        public FakeQueryPart(IEnumerable<(int itemId, IndexedWord[] indexedWordLocations)> matches)
        {
            this.results = new IntermediateQueryResult(matches
                .Select(m => (m.itemId, (IEnumerable<IndexedWord>)m.indexedWordLocations)));
        }

        public FakeQueryPart(params int[] matchedItems)
        {
            this.results = new IntermediateQueryResult(
                matchedItems.Select(
                    m => (m, (IEnumerable<IndexedWord>)new[] { new IndexedWord((byte)m, new[] { new WordLocation(m, m, m) }) })));
        }

        public IntermediateQueryResult Evaluate(Func<IIndexNavigator> navigatorCreator)
        {
            return this.results;
        }
    }
}

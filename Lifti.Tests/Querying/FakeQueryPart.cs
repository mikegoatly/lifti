using Lifti.Querying;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Lifti.Tests.Querying
{
    public class FakeQueryPart : IQueryPart
    {
        private IntermediateQueryResult results;

        public FakeQueryPart(IEnumerable<(int itemId, IndexedWordLocation[] indexedWordLocations)> matches)
        {
            this.results = new IntermediateQueryResult(matches
                .Select(m => (m.itemId, (IEnumerable<IndexedWordLocation>)m.indexedWordLocations)));
        }

        public FakeQueryPart(params int[] matchedItems)
        {
            this.results = new IntermediateQueryResult(
                matchedItems.Select(
                    m => (m, (IEnumerable<IndexedWordLocation>)new[] { new IndexedWordLocation((byte)m, new[] { new Range(m, m) }) })));
        }

        public IntermediateQueryResult Evaluate(Func<IIndexNavigator> navigatorCreator)
        {
            return this.results;
        }
    }
}

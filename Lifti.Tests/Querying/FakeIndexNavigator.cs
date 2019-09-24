using Lifti.Querying;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Lifti.Tests.Querying
{
    public class FakeIndexNavigator : IIndexNavigator
    {
        private IntermediateQueryResult matches;

        public FakeIndexNavigator(params int[] matchedItems)
        {
            this.matches = new IntermediateQueryResult(
                matchedItems.Select(
                    m => (m, (IEnumerable<IndexedWordLocation>)new[] { new IndexedWordLocation((byte)m, new[] { new Range(m, m) }) })));
        }

        public IntermediateQueryResult GetExactMatches()
        {
            return this.matches;
        }

        public bool Process(char next)
        {
            return true;
        }

        public bool Process(ReadOnlySpan<char> text)
        {
            return true;
        }
    }
}

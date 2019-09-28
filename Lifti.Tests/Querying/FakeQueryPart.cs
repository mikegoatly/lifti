using Lifti.Querying;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Lifti.Tests.Querying
{
    public class FakeQueryPart : IQueryPart
    {
        private IntermediateQueryResult results;

        public FakeQueryPart(params QueryWordMatch[] matches)
        {
            this.results = new IntermediateQueryResult(matches);
        }

        public FakeQueryPart(params int[] matchedItems)
        {
            this.results = new IntermediateQueryResult(
                matchedItems.Select(
                    m => new QueryWordMatch(m, new[] { new FieldMatch((byte)m, new[] { new WordLocation(m, m, m) }) })));
        }

        public IntermediateQueryResult Evaluate(Func<IIndexNavigator> navigatorCreator)
        {
            return this.results;
        }
    }
}

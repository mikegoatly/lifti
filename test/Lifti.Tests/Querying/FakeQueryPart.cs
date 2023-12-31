using Lifti.Querying;
using Lifti.Querying.QueryParts;
using System;
using System.Linq;

namespace Lifti.Tests.Querying
{
    public class FakeQueryPart : QueryTestBase, IQueryPart
    {
        private readonly IntermediateQueryResult results;

        public FakeQueryPart(params ScoredToken[] matches)
        {
            this.results = new IntermediateQueryResult(matches);
        }

        public FakeQueryPart(params int[] matchedItems)
        {
            this.results = new IntermediateQueryResult(
                matchedItems.Select(
                    m => new ScoredToken(
                        m,
                        new[] { ScoredFieldMatch(m, (byte)m, m) })));
        }

        public IntermediateQueryResult Evaluate(Func<IIndexNavigator> navigatorCreator, QueryContext queryContext)
        {
            return this.results;
        }
    }
}

using Lifti.Querying;
using Lifti.Querying.QueryParts;
using System;
using System.Linq;

namespace Lifti.Tests.Querying
{
    public class FakeQueryPart : QueryTestBase, IQueryPart, IWordQueryPart
    {
        private readonly IntermediateQueryResult results;

        public FakeQueryPart(params QueryWordMatch[] matches)
        {
            this.results = new IntermediateQueryResult(matches);
        }

        public FakeQueryPart(params int[] matchedItems)
        {
            this.results = new IntermediateQueryResult(
                matchedItems.Select(
                    m => new QueryWordMatch(m, new[] { FieldMatch((byte)m, m) })));
        }

        public string Word => throw new NotImplementedException();

        public IntermediateQueryResult Evaluate(Func<IIndexNavigator> navigatorCreator, IQueryContext queryContext)
        {
            return this.results;
        }
    }
}

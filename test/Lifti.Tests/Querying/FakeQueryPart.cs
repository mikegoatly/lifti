using Lifti.Querying;
using Lifti.Querying.QueryParts;
using System;
using System.Linq;

namespace Lifti.Tests.Querying
{
    public class FakeQueryPart : QueryTestBase, IQueryPart
    {
        private readonly IntermediateQueryResult results;
        private readonly double weighting = 1D;

        public FakeQueryPart(params ScoredToken[] matches)
        {
            this.results = new IntermediateQueryResult(matches.ToList(), false);
        }

        public FakeQueryPart(double weighting, params ScoredToken[] matches)
            : this(matches)
        {
            this.weighting = weighting;
        }

        public FakeQueryPart(params int[] matchedItems)
        {
            this.results = new IntermediateQueryResult(
                matchedItems.Select(
                    m => new ScoredToken(
                        m,
                        new[] { ScoredFieldMatch(m, (byte)m, m) }))
                .ToList(),
                false);
        }

        public double CalculateWeighting(Func<IIndexNavigator> navigatorCreator)
        {
            return this.weighting;
        }

        public IntermediateQueryResult Evaluate(Func<IIndexNavigator> navigatorCreator, QueryContext queryContext)
        {
            return this.results;
        }
    }
}

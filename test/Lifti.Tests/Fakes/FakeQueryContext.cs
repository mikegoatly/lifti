using Lifti.Querying;
using Lifti.Querying.QueryParts;

namespace Lifti.Tests.Fakes
{
    public class FakeQueryContext : IQueryContext
    {
        private readonly IntermediateQueryResult result;

        public FakeQueryContext()
            : this(new IntermediateQueryResult())
        {
        }

        public FakeQueryContext(IntermediateQueryResult result)
        {
            this.result = result;
        }

        public IntermediateQueryResult ApplyTo(IntermediateQueryResult intermediateQueryResult)
        {
            return this.result;
        }

        public void ApplyTo(MatchCollector matchCollector)
        {
        }
    }
}

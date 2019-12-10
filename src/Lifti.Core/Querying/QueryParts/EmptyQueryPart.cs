using System;

namespace Lifti.Querying.QueryParts
{
    public class EmptyQueryPart : IQueryPart
    {
        private EmptyQueryPart()
        {
        }

        public static EmptyQueryPart Instance { get; } = new EmptyQueryPart();

        public IntermediateQueryResult Evaluate(Func<IIndexNavigator> navigatorCreator, IQueryContext queryContext)
        {
            return IntermediateQueryResult.Empty;
        }
    }
}

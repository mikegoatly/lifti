using System;

namespace Lifti.Querying.QueryParts
{
    public class BracketedQueryPart : IQueryPart
    {
        public BracketedQueryPart(IQueryPart statement)
        {
            this.Statement = statement;
        }

        public IQueryPart Statement
        {
            get;
        }

        public IntermediateQueryResult Evaluate(Func<IIndexNavigator> navigatorCreator, IQueryContext queryContext)
        {
            return this.Statement.Evaluate(navigatorCreator, queryContext);
        }

        public override string ToString()
        {
            return $"({this.Statement})";
        }
    }
}

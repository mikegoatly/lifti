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

        public IntermediateQueryResult Evaluate(Func<IIndexNavigator> navigatorCreator)
        {
            return this.Statement.Evaluate(navigatorCreator);
        }

        public override string ToString()
        {
            return $"({this.Statement})";
        }
    }
}

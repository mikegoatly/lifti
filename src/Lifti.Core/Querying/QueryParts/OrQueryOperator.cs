using System;

namespace Lifti.Querying.QueryParts
{
    public class OrQueryOperator : BinaryQueryOperator
    {
        public OrQueryOperator(IQueryPart left, IQueryPart right)
            : base(left, right)
        {
        }

        public override OperatorPrecedence Precedence => OperatorPrecedence.And;

        public override IntermediateQueryResult Evaluate(Func<IIndexNavigator> navigatorCreator, IQueryContext queryContext)
        {
            return this.Left.Evaluate(navigatorCreator, queryContext)
                .Union(this.Right.Evaluate(navigatorCreator, queryContext));
        }

        public override string ToString()
        {
            return $"{this.Left} | {this.Right}";
        }
    }
}

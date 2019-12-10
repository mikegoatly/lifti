using System;

namespace Lifti.Querying.QueryParts
{
    public class AndQueryOperator : BinaryQueryOperator
    {
        public AndQueryOperator(IQueryPart left, IQueryPart right)
            : base(left, right)
        {
        }

        public override OperatorPrecedence Precedence => OperatorPrecedence.And;

        public override IntermediateQueryResult Evaluate(Func<IIndexNavigator> navigatorCreator, IQueryContext queryContext)
        {
            return this.Left.Evaluate(navigatorCreator, queryContext)
                .Intersect(this.Right.Evaluate(navigatorCreator, queryContext));
        }

        public override string ToString()
        {
            return this.Left + " & " + this.Right;
        }
    }

}

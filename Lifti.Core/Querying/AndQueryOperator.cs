using System;

namespace Lifti.Querying
{
    public class AndQueryOperator : BinaryQueryOperator
    {
        public AndQueryOperator(IQueryPart left, IQueryPart right)
            : base(left, right)
        {
        }

        public override OperatorPrecedence Precedence => OperatorPrecedence.And;

        public override IntermediateQueryResult Evaluate(Func<IIndexNavigator> navigatorCreator)
        {
            return this.Left.Evaluate(navigatorCreator).Intersect(this.Right.Evaluate(navigatorCreator));
        }

        public override string ToString()
        {
            return "(" + this.Left + " AND " + this.Right + ")";
        }
    }

}

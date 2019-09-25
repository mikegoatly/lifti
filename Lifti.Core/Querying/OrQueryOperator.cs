using System;

namespace Lifti.Querying
{
    public class OrQueryOperator : BinaryQueryOperator
    {
        public OrQueryOperator(IQueryPart left, IQueryPart right)
            : base(left, right)
        {
        }

        public override OperatorPrecedence Precedence => OperatorPrecedence.And;

        public override IntermediateQueryResult Evaluate(Func<IIndexNavigator> navigatorCreator)
        {
            return this.Left.Evaluate(navigatorCreator).Union(this.Right.Evaluate(navigatorCreator));
        }

        public override string ToString()
        {
            return "(" + this.Left + " OR " + this.Right + ")";
        }
    }

}

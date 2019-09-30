using System;

namespace Lifti.Querying.QueryParts
{
    public class PrecedingQueryOperator : BinaryQueryOperator
    {
        public PrecedingQueryOperator(IQueryPart left, IQueryPart right)
            : base(left, right)
        {
        }

        public override OperatorPrecedence Precedence => OperatorPrecedence.Positional;

        public override IntermediateQueryResult Evaluate(Func<IIndexNavigator> navigatorCreator)
        {
            return this.Left.Evaluate(navigatorCreator).PositionalIntersect(this.Right.Evaluate(navigatorCreator), int.MaxValue, 0);
        }
    }
}

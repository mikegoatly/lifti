using System;
using System.Globalization;

namespace Lifti.Querying.QueryParts
{
    public class PrecedingNearQueryOperator : BinaryQueryOperator
    {
        public PrecedingNearQueryOperator(IQueryPart left, IQueryPart right, int tolerance = 5)
            : base(left, right)
        {
            this.Tolerance = tolerance;
        }

        public override OperatorPrecedence Precedence => OperatorPrecedence.Positional;

        public int Tolerance { get; }

        public override IntermediateQueryResult Evaluate(Func<IIndexNavigator> navigatorCreator)
        {
            return this.Left.Evaluate(navigatorCreator).CompositePositionalIntersect(this.Right.Evaluate(navigatorCreator), 0, this.Tolerance);
        }

        public override string ToString()
        {
            var toleranceText = this.Tolerance == 5 ? string.Empty : this.Tolerance.ToString(CultureInfo.InvariantCulture);
            return $"{this.Left} ~{toleranceText}> {this.Right}";
        }
    }
}

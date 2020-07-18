using System;

namespace Lifti.Querying.QueryParts
{
    /// <summary>
    /// A query part that filters matched items to only those matched as an intersection of two <see cref="IQueryPart"/>s.
    /// </summary>
    public class AndQueryOperator : BinaryQueryOperator
    {
        /// <summary>
        /// Constructs a new instance of <see cref="AndQueryOperator"/>.
        /// </summary>
        public AndQueryOperator(IQueryPart left, IQueryPart right)
            : base(left, right)
        {
        }

        /// <inheritdoc/>
        public override OperatorPrecedence Precedence => OperatorPrecedence.And;

        /// <inheritdoc/>
        public override IntermediateQueryResult Evaluate(Func<IIndexNavigator> navigatorCreator, IQueryContext queryContext)
        {
            return this.Left.Evaluate(navigatorCreator, queryContext)
                .Intersect(this.Right.Evaluate(navigatorCreator, queryContext));
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return this.Left + " & " + this.Right;
        }
    }

}

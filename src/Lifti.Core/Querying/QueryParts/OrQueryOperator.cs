using System;

namespace Lifti.Querying.QueryParts
{
    /// <summary>
    /// An <see cref="IQueryPart"/> that produces a union of the results in two other <see cref="IQueryPart"/>s.
    /// </summary>
    public class OrQueryOperator : BinaryQueryOperator
    {
        /// <summary>
        /// Constructs a new <see cref="OrQueryOperator"/>.
        /// </summary>
        public OrQueryOperator(IQueryPart left, IQueryPart right)
            : base(left, right)
        {
        }

        /// <inheritdoc/>
        public override OperatorPrecedence Precedence => OperatorPrecedence.And;

        /// <inheritdoc/>
        public override IntermediateQueryResult Evaluate(Func<IIndexNavigator> navigatorCreator, IQueryContext queryContext)
        {
            return this.Left.Evaluate(navigatorCreator, queryContext)
                .Union(this.Right.Evaluate(navigatorCreator, queryContext));
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"{this.Left} | {this.Right}";
        }
    }
}

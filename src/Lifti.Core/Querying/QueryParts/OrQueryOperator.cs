using System;
using System.Collections.Generic;

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
        public override IntermediateQueryResult Evaluate(Func<IIndexNavigator> navigatorCreator, QueryContext queryContext)
        {
            return this.Left.Evaluate(navigatorCreator, queryContext)
                .Union(this.Right.Evaluate(navigatorCreator, queryContext));
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"{this.Left} | {this.Right}";
        }

        /// <inheritdoc/>
        protected override double RunWeightingCalculation(Func<IIndexNavigator> navigatorCreator)
        {
            // Both sides of the OR operator are evaluated, so the weighting is the sum of the two
            return this.Left.CalculateWeighting(navigatorCreator) + this.Right.CalculateWeighting(navigatorCreator);
        }

        /// <summary>
        /// Combines all the given query parts with <see cref="OrQueryOperator"/>s. If <paramref name="queryParts"/> contains a single element, then
        /// that query part is returned unaltered, making this effectively a no-op.
        /// </summary>
        /// <exception cref="QueryParserException">Thrown when <paramref name="queryParts"/> is empty.</exception>
        public static IQueryPart CombineAll(IEnumerable<IQueryPart> queryParts)
        {
            if (queryParts is null)
            {
                throw new ArgumentNullException(nameof(queryParts));
            }

            IQueryPart? current = null;
            foreach (var queryPart in queryParts)
            {
                if (current == null)
                {
                    current = queryPart;
                }
                else
                {
                    current = new OrQueryOperator(current, queryPart);
                }
            }

            if (current == null)
            {
                throw new QueryParserException(ExceptionMessages.CannotCombineAnEmptySetOfQueryParts);
            }

            return current;
        }
    }
}

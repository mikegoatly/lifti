using System;
using System.Collections.Generic;

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

        /// <summary>
        /// Combines all the given query parts with <see cref="AndQueryOperator"/>s. If <paramref name="queryParts"/> contains a single element, then
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
                    current = new AndQueryOperator(current, queryPart);
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

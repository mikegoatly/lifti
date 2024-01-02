using Lifti.Querying.QueryParts;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Lifti.Querying.QueryParts
{
    /// <summary>
    /// An abstract <see cref="IQueryPart"/> representing a binary query operator, e.g. an AND or an OR operator.
    /// </summary>
    public abstract class BinaryQueryOperator : IBinaryQueryOperator
    {
        /// <summary>
        /// Constructs a new instance of <see cref="BinaryQueryOperator"/>.
        /// </summary>
        protected BinaryQueryOperator(IQueryPart left, IQueryPart right)
        {
            this.Left = left;
            this.Right = right;
        }

        /// <inheritdoc/>
        public IQueryPart Left
        {
            get;
            set;
        }

        /// <inheritdoc/>
        public IQueryPart Right
        {
            get;
            set;
        }

        /// <inheritdoc/>
        public abstract OperatorPrecedence Precedence
        {
            get;
        }

        /// <inheritdoc/>
        public abstract IntermediateQueryResult Evaluate(Func<IIndexNavigator> navigatorCreator, QueryContext queryContext);

        /// <summary>
        /// Evaluates the left and right sides of the query, attempting to optimise the results so that the second evaluation
        /// is filtered to only those documents that matched the first.
        /// </summary>
        protected (IntermediateQueryResult leftResults, IntermediateQueryResult rightResults) EvaluateWithDocumentIntersection(
            Func<IIndexNavigator> navigatorCreator, 
            QueryContext queryContext)
        {
            if (queryContext is null)
            {
                throw new ArgumentNullException(nameof(queryContext));
            }

            // TODO - if we can somehow weight the two sides, we can optimize this by evaluating the cheapest side first
            var leftResults = this.Left.Evaluate(navigatorCreator, queryContext);
            var rightResults = this.Right.Evaluate(
                navigatorCreator,
                // Filter the right side to only those documents that matched the left side
                queryContext with { FilterToDocumentIds = leftResults.ToDocumentIdLookup() });

            return (leftResults, rightResults);
        }
    }

}

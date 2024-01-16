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
        private double? weighting;

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

        /// <inheritdoc/>
        public double CalculateWeighting(Func<IIndexNavigator> navigatorCreator)
        {
            this.weighting ??= this.RunWeightingCalculation(navigatorCreator);
            return this.weighting.GetValueOrDefault();
        }

        /// <summary>
        /// Runs the weighting calculation for this query part.
        /// </summary>
        protected virtual double RunWeightingCalculation(Func<IIndexNavigator> navigatorCreator)
        {
            // Most binary operators are intersections, so we will use the weighting of the cheapest side
            return Math.Min(this.Left.CalculateWeighting(navigatorCreator), this.Right.CalculateWeighting(navigatorCreator));
        }

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

            // Work out which side is cheapest to evaluate first
            var leftWeighting = this.Left.CalculateWeighting(navigatorCreator);
            var rightWeighting = this.Right.CalculateWeighting(navigatorCreator);

            IntermediateQueryResult leftResults;
            IntermediateQueryResult rightResults;

            if (leftWeighting <= rightWeighting)
            {
                leftResults = this.Left.Evaluate(navigatorCreator, queryContext);
                rightResults = this.Right.Evaluate(
                    navigatorCreator,
                    // Filter the right side to only those documents that matched the left side
                    queryContext with { FilterToDocumentIds = leftResults.ToDocumentIdLookup() });
            }
            else
            {
                rightResults = this.Right.Evaluate(navigatorCreator, queryContext);
                leftResults = this.Left.Evaluate(
                    navigatorCreator,
                    // Filter the left side to only those documents that matched the right side
                    queryContext with { FilterToDocumentIds = rightResults.ToDocumentIdLookup() });
            }
            

            return (leftResults, rightResults);
        }
    }

}

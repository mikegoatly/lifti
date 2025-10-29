using System;
using System.Collections.Generic;
using System.Linq;

namespace Lifti.Querying.QueryParts
{
    /// <summary>
    /// A query part that filters matched documents to exclude those matched by the right operand.
    /// This is equivalent to an AND NOT operation: documents matching the left operand, excluding
    /// any that also match the right operand.
    /// </summary>
    public sealed class AndNotQueryOperator : BinaryQueryOperator
    {
        /// <summary>
        /// Constructs a new instance of <see cref="AndNotQueryOperator"/>.
        /// </summary>
        public AndNotQueryOperator(IQueryPart left, IQueryPart right)
            : base(left, right)
        {
        }

        /// <inheritdoc/>
        public override OperatorPrecedence Precedence => OperatorPrecedence.Or;

        /// <inheritdoc/>
        public override IntermediateQueryResult Evaluate(Func<IIndexNavigator> navigatorCreator, QueryContext queryContext)
        {
            // Evaluate left side first
            var leftResults = this.Left.Evaluate(navigatorCreator, queryContext);

            if (leftResults.Matches.Count == 0)
            {
                // Nothing to subtract from
                return IntermediateQueryResult.Empty;
            }

            // Evaluate right side, but only for documents that matched left (optimization)
            // If there's already a filter, intersect with it
            var documentFilter = leftResults.ToDocumentIdLookup();
            if (queryContext.FilterToDocumentIds != null)
            {
                documentFilter = [.. queryContext.FilterToDocumentIds.Intersect(documentFilter)];
            }

            var rightResults = this.Right.Evaluate(
                navigatorCreator,
                queryContext with { FilterToDocumentIds = documentFilter });

            var timing = queryContext.ExecutionTimings.Start(this, queryContext);
            var results = leftResults.Except(rightResults);
            return timing.Complete(results);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"{this.Left} &! {this.Right}";
        }
    }
}

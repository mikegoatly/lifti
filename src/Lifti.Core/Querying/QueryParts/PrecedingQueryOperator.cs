using System;

namespace Lifti.Querying.QueryParts
{
    /// <summary>
    /// An <see cref="IQueryPart"/> that produces an intersection of two <see cref="IQueryPart"/>s, restricting
    /// a document's field matches such that the locations of the first appear before the locations of the second. 
    /// Documents that result in no field matches are filtered out.
    /// </summary>
    public sealed class PrecedingQueryOperator : BinaryQueryOperator
    {
        /// <summary>
        /// Constructs a new instance of <see cref="PrecedingQueryOperator"/>.
        /// </summary>
        public PrecedingQueryOperator(IQueryPart left, IQueryPart right)
            : base(left, right)
        {
        }

        /// <inheritdoc/>
        public override OperatorPrecedence Precedence => OperatorPrecedence.Positional;

        /// <inheritdoc/>
        public override IntermediateQueryResult Evaluate(Func<IIndexNavigator> navigatorCreator, QueryContext queryContext)
        {
            var (leftResults, rightResults) = this.EvaluateWithDocumentIntersection(navigatorCreator, queryContext with {  ParentQueryPart = this });

            var timing = queryContext.ExecutionTimings.Start(this, queryContext);
            var results = leftResults.PrecedingIntersect(rightResults);
            return timing.Complete(results);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"{this.Left} > {this.Right}";
        }
    }
}

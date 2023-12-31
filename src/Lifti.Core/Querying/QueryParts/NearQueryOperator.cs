using System;
using System.Globalization;

namespace Lifti.Querying.QueryParts
{
    /// <summary>
    /// An <see cref="IQueryPart"/> that produces an intersection of two <see cref="IQueryPart"/>s, restricting
    /// a document's field matches such that the locations are close to one another. Documents that result in no field matches
    /// are filtered out.
    /// </summary>
    public class NearQueryOperator : BinaryQueryOperator
    {
        /// <summary>
        /// Constructs a new instance of <see cref="NearQueryOperator"/>.
        /// </summary>
        public NearQueryOperator(IQueryPart left, IQueryPart right, int tolerance = 5)
            : base(left, right)
        {
            this.Tolerance = tolerance;
        }

        /// <inheritdoc/>
        public override OperatorPrecedence Precedence => OperatorPrecedence.Positional;

        /// <summary>
        /// Gets the tolerance for the operator, i.e. the maximum difference allowed between words in a matched document.
        /// </summary>
        public int Tolerance { get; }

        /// <inheritdoc/>
        public override IntermediateQueryResult Evaluate(Func<IIndexNavigator> navigatorCreator, QueryContext queryContext)
        {
            return this.Left.Evaluate(navigatorCreator, queryContext)
                .CompositePositionalIntersect(
                    this.Right.Evaluate(navigatorCreator, queryContext),
                    this.Tolerance,
                    this.Tolerance);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            var toleranceText = this.Tolerance == 5 ? string.Empty : this.Tolerance.ToString(CultureInfo.InvariantCulture);
            return $"{this.Left} ~{toleranceText} {this.Right}";
        }
    }
}

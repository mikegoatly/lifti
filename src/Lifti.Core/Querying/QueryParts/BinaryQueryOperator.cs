using System;

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
    }

}

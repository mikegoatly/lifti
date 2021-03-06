﻿using System;

namespace Lifti.Querying.QueryParts
{
    /// <summary>
    /// An <see cref="IQueryPart"/> that produces an intersection of two <see cref="IQueryPart"/>s, restricting
    /// an item's field matches such that the locations of the first appear before the locations of the second. 
    /// Items that result in no field matches are filtered out.
    /// </summary>
    public class PrecedingQueryOperator : BinaryQueryOperator
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
        public override IntermediateQueryResult Evaluate(Func<IIndexNavigator> navigatorCreator, IQueryContext queryContext)
        {
            return this.Left.Evaluate(navigatorCreator, queryContext)
                .PrecedingIntersect(this.Right.Evaluate(navigatorCreator, queryContext));
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"{this.Left} > {this.Right}";
        }
    }
}

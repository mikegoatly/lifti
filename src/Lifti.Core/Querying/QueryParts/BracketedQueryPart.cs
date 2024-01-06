﻿using System;

namespace Lifti.Querying.QueryParts
{
    /// <summary>
    /// An <see cref="IQueryPart"/> that groups other statements together.
    /// </summary>
    public sealed class BracketedQueryPart : IQueryPart
    {
        /// <summary>
        /// Constructs a new instance of <see cref="BracketedQueryPart"/>.
        /// </summary>
        /// <param name="statement"></param>
        public BracketedQueryPart(IQueryPart statement)
        {
            this.Statement = statement;
        }

        /// <summary>
        /// Gets the <see cref="IQueryPart"/> that this instance wraps.
        /// </summary>
        public IQueryPart Statement
        {
            get;
        }

        /// <inheritdoc/>
        public IntermediateQueryResult Evaluate(Func<IIndexNavigator> navigatorCreator, QueryContext queryContext)
        {
            return this.Statement.Evaluate(navigatorCreator, queryContext);
        }

        /// <inheritdoc/>
        public double CalculateWeighting(Func<IIndexNavigator> navigatorCreator)
        {
            // Just defer to the weighting of the statement
            return this.Statement.CalculateWeighting(navigatorCreator);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"({this.Statement})";
        }
    }
}

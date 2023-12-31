using System;

namespace Lifti.Querying.QueryParts
{
    /// <summary>
    /// An <see cref="IQueryPart"/> that groups other statements together.
    /// </summary>
    public class BracketedQueryPart : IQueryPart
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
        public override string ToString()
        {
            return $"({this.Statement})";
        }
    }
}

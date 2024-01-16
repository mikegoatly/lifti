using System;

namespace Lifti.Querying.QueryParts
{
    /// <summary>
    /// A special case <see cref="IQueryPart"/> used to represent an empty query.
    /// </summary>
    public sealed class EmptyQueryPart : IQueryPart
    {
        private EmptyQueryPart()
        {
        }

        /// <summary>
        /// Gets the singleton <see cref="EmptyQueryPart"/> instance.
        /// </summary>
        public static EmptyQueryPart Instance { get; } = new EmptyQueryPart();

        /// <inheritdoc />
        public IntermediateQueryResult Evaluate(Func<IIndexNavigator> navigatorCreator, QueryContext queryContext)
        {
            return IntermediateQueryResult.Empty;
        }

        /// <inheritdoc />
        public double CalculateWeighting(Func<IIndexNavigator> navigatorCreator)
        {
            return 0;
        }
    }
}

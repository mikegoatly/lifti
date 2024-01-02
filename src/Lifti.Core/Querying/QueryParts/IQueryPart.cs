using System;

namespace Lifti.Querying.QueryParts
{
    /// <summary>
    /// Defines a part of a query to be evaluated against an index.
    /// </summary>
    public interface IQueryPart
    {
        /// <summary>
        /// Executes this query part instance against the specified query context.
        /// </summary>
        /// <param name="navigatorCreator">
        /// A delegate capable of creating an <see cref="IndexNavigator"/> for the index
        /// being queried.
        /// </param>
        /// <param name="queryContext">
        /// The current <see cref="QueryContext"/>.
        /// </param>
        /// <returns>
        /// The <see cref="IntermediateQueryResult"/> that contains the matches.
        /// </returns>
        IntermediateQueryResult Evaluate(Func<IIndexNavigator> navigatorCreator, QueryContext queryContext);

        /// <summary>
        /// Calculates the execution cost weighting for this query part. The lower the weighting the 
        /// cheaper the query part is to execute.
        /// </summary>
        /// <param name="navigatorCreator">
        /// A delegate capable of creating an <see cref="IndexNavigator"/> for the index
        /// being queried.
        /// </param>
        double CalculateWeighting(Func<IIndexNavigator> navigatorCreator);
    }
}

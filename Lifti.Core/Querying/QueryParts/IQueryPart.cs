using System;

namespace Lifti.Querying.QueryParts
{
    public interface IQueryPart
    {
        /// <summary>
        /// Executes this query part instance against the specified query context.
        /// </summary>
        /// <typeparam name="TItem">The type of the item.</typeparam>
        /// <param name="context">The query context to execute the query against.</param>
        /// <returns>
        /// The query result that contains the matched items.
        /// </returns>
        IntermediateQueryResult Evaluate(Func<IIndexNavigator> navigatorCreator);
    }
}

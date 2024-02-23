using System;

namespace Lifti
{
    /// <summary>
    /// Options to apply when executing a query.
    /// </summary>
    [Flags]
    public enum QueryExecutionOptions
    {
        /// <summary>
        /// No options are applied.
        /// </summary>
        None = 0,

        /// <summary>
        /// Information about the performance of the query will be collected during execution and
        /// <see cref="ISearchResults{TKey}.GetExecutionPlan"/> will be available on the results.
        /// Without this option, the execution plan will be empty if requested.
        /// </summary>
        IncludeExecutionPlan = 1
    }
}
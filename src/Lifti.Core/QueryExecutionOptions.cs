using System;
using System.Collections.Generic;

namespace Lifti
{
    /// <summary>
    /// Options to apply when executing a query.
    /// </summary>
    public record QueryExecutionOptions
    {
        /// <summary>
        /// The default options to apply when executing a query.
        /// </summary>
        public static readonly QueryExecutionOptions Default = new();

        /// <summary>
        /// Information about the performance of the query will be collected during execution and
        /// <see cref="ISearchResults{TKey}.GetExecutionPlan"/> will be available on the results.
        /// Without this option, the execution plan will be empty if requested.
        /// </summary>
        public bool IncludeExecutionPlan { get; init; }

        /// <summary>
        /// Gets or sets the collection of field names to restrict search operations to.
        /// </summary>
        public ICollection<string>? SearchInFields { get; init; }
    }
}
using System.Collections.Generic;

namespace Lifti.Querying
{
    using DocumentIdSet = IReadOnlySet<int>;
    using FieldIdSet = IReadOnlySet<byte>;

    /// <summary>
    /// Manages context during the execution of a query, allowing for aspects like field filters to be applied.
    /// </summary>
    public sealed record QueryContext(
        byte? FilterToFieldId = null, 
        DocumentIdSet? FilterToDocumentIds = null)
    {
        /// <summary>
        /// Gets an empty query context.
        /// </summary>
        public static QueryContext Empty => new();

        /// <summary>
        /// If set, restricts the results of the query to only those that include matching tokens in the specified field.
        /// </summary>
        public FieldIdSet? GlobalFieldFilters { get; init; }

        internal ExecutionTimings ExecutionTimings { get; init; } = ExecutionTimings.NullTimings;
    }
}

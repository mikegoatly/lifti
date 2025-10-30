using System.Collections.Generic;

namespace Lifti.Querying
{
    using DocumentIdSet = IReadOnlySet<int>;

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

        internal ExecutionTimings ExecutionTimings { get; init; } = ExecutionTimings.NullTimings;
    }
}

using System;
using System.Collections.Generic;
using System.Linq;

namespace Lifti.Querying
{
#if NETSTANDARD
    using DocumentIdSet = ISet<int>;
#else
    using DocumentIdSet = IReadOnlySet<int>;
#endif

    /// <summary>
    /// Manages context during the execution of a query, allowing for aspects like field filters to be applied.
    /// </summary>
    public sealed record QueryContext(byte? FilterToFieldId = null, DocumentIdSet? FilterToDocumentIds = null)
    {
        /// <summary>
        /// Gets an empty query context.
        /// </summary>
        public static QueryContext Empty { get; } = new();
    }
}

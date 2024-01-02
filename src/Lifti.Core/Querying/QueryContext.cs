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

        ///// <summary>
        ///// Applies any additional filters present in the current query context, e.g. field filters, 
        ///// to the given <see cref="IntermediateQueryResult"/>, returning a new <see cref="IntermediateQueryResult"/> instance.
        ///// </summary>
        //public IntermediateQueryResult ApplyTo(IntermediateQueryResult intermediateQueryResult)
        //{
        //    if (this.FilterToFieldId is not byte targetFieldId)
        //    {
        //        return intermediateQueryResult;
        //    }

        //    return new IntermediateQueryResult(
        //        intermediateQueryResult.Matches
        //            .Select(m => new ScoredToken(
        //                m.DocumentId,
        //                m.FieldMatches.Where(fm => fm.FieldId == targetFieldId).ToList()))
        //            .Where(m => m.FieldMatches.Count > 0),
        //        true);
        //}
    }
}

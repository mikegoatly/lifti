using System;
using System.Linq;

namespace Lifti.Querying
{
    /// <summary>
    /// Manages context during the execution of a query, allowing for aspects like field filters to be applied.
    /// </summary>
    public record struct QueryContext(byte? FilterToFieldId = null)
    {
        /// <summary>
        /// Gets an empty query context.
        /// </summary>
        public static QueryContext Empty { get; }

        /// <summary>
        /// Applies any additional filters present in the current query context, e.g. field filters,
        /// to the given <see cref="MatchCollector"/>, which is mutated as required.
        /// </summary>
        public void ApplyTo(MatchCollector matchCollector)
        {
            if (matchCollector is null)
            {
                throw new ArgumentNullException(nameof(matchCollector));
            }

            if (this.FilterToFieldId is not byte targetFieldId)
            {
                return;
            }

            foreach (var match in matchCollector.CollectedMatches)
            {
                match.Value.RemoveAll(fm => fm.FieldId != targetFieldId);
            }
        }

        /// <summary>
        /// Applies any additional filters present in the current query context, e.g. field filters, 
        /// to the given <see cref="IntermediateQueryResult"/>, returning a new <see cref="IntermediateQueryResult"/> instance.
        /// </summary>
        public IntermediateQueryResult ApplyTo(IntermediateQueryResult intermediateQueryResult)
        {
            if (this.FilterToFieldId is not byte targetFieldId)
            {
                return intermediateQueryResult;
            }

            return new IntermediateQueryResult(
                intermediateQueryResult.Matches
                    .Select(m => new ScoredToken(
                        m.DocumentId,
                        m.FieldMatches.Where(fm => fm.FieldId == targetFieldId).ToList()))
                    .Where(m => m.FieldMatches.Count > 0),
                true);
        }
    }
}

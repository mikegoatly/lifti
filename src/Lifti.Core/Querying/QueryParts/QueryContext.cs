using System.Collections.Generic;
using System.Linq;

namespace Lifti.Querying.QueryParts
{
    internal class QueryContext : IQueryContext
    {
        private readonly byte? filterToFieldId;

        public static IQueryContext Empty { get; } = new QueryContext(null);

        private QueryContext(byte? filterToFieldId)
        {
            this.filterToFieldId = filterToFieldId;
        }

        public void ApplyTo(MatchCollector matchCollector)
        {
            if (this.filterToFieldId == null)
            {
                return;
            }

            foreach (var match in matchCollector.CollectedMatches)
            {
                match.Value.RemoveAll(fm => fm.FieldId != this.filterToFieldId);
            }
        }

        /// <inheritdoc />
        public IntermediateQueryResult ApplyTo(IntermediateQueryResult intermediateQueryResult)
        {
            if (this.filterToFieldId == null)
            {
                return intermediateQueryResult;
            }

            return new IntermediateQueryResult(
                intermediateQueryResult.Matches
                    .Select(m => new ScoredToken(
                        m.DocumentId,
                        m.FieldMatches.Where(fm => fm.FieldId == this.filterToFieldId).ToList()))
                    .Where(m => m.FieldMatches.Count > 0),
                true);
        }

        public static IQueryContext Create(IQueryContext currentContext, byte? filterToFieldId = null)
        {
            if (filterToFieldId == null)
            {
                return currentContext;
            }

            return new QueryContext(filterToFieldId);
        }
    }
}

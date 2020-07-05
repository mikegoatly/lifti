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

        /// <inheritdoc />
        public IntermediateQueryResult ApplyTo(IntermediateQueryResult intermediateQueryResult)
        {
            if (this.filterToFieldId == null)
            {
                return intermediateQueryResult;
            }

            return new IntermediateQueryResult(
                intermediateQueryResult.Matches
                    .Select(m => new QueryWordMatch(
                        m.ItemId,
                        m.FieldMatches.Where(fm => fm.FieldId == this.filterToFieldId).ToList()))
                    .Where(m => m.FieldMatches.Count > 0));
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

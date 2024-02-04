using Lifti.Querying.QueryParts;
using System;

namespace Lifti.Querying
{
    internal class QueryPartExecutionDetails
    {
        public QueryPartExecutionDetails(
            IQueryPart queryPart, 
            TimeSpan executionTime, 
            int documentCount,
            int? documentFiltersApplied,
            int? fieldFiltersApplied)
        {
            this.QueryPart = queryPart;
            this.ExecutionTime = executionTime;
            this.DocumentCount = documentCount;
            this.DocumentFiltersApplied = documentFiltersApplied;
            this.FieldFiltersApplied = fieldFiltersApplied;
        }

        public IQueryPart QueryPart { get; }
        public TimeSpan ExecutionTime { get; }
        public int DocumentCount { get; }
        public int? DocumentFiltersApplied { get; }
        public int? FieldFiltersApplied { get; }
    }
}

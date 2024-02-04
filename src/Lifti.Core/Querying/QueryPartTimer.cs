using Lifti.Querying.QueryParts;
using System;
using System.Diagnostics;

namespace Lifti.Querying
{
    internal class QueryPartTimer
    {
        private static readonly SharedPool<QueryPartTimer> pool = new(static () => new QueryPartTimer(), static (o) => { }, 10);
        private static readonly double timestampToTicks = TimeSpan.TicksPerSecond / (double)Stopwatch.Frequency;
        private ExecutionTimings timingsContainer = null!;

        public int? DocumentFiltersApplied { get; private set; }
        public int? FieldFiltersApplied { get; private set; }
        private QueryContext queryContext = null!;
        private IQueryPart queryPart = null!;
        private long startTimestamp;

        private QueryPartTimer()
        {
        }

        public static QueryPartTimer StartNew(ExecutionTimings parent, IQueryPart queryPart, QueryContext currentContext)
        {
            return Start(parent, queryPart, currentContext);
        }

        private static QueryPartTimer Start(ExecutionTimings parent, IQueryPart queryPart, QueryContext currentContext)
        {
            var timer = pool.Take();
            timer.timingsContainer = parent;
            timer.DocumentFiltersApplied = currentContext.FilterToDocumentIds?.Count ?? null;
            timer.FieldFiltersApplied = currentContext.FilterToFieldId.HasValue ? 1 : null;
            timer.queryPart = queryPart;
            timer.queryContext = currentContext;
            timer.startTimestamp = Stopwatch.GetTimestamp();
            return timer;
        }

        private long CalculateTicks()
        {
            return Stopwatch.GetTimestamp() - this.startTimestamp;
        }

        public void Resume()
        {
            this.startTimestamp = Stopwatch.GetTimestamp();
        }

        public IntermediateQueryResult Complete(IntermediateQueryResult result)
        {
            var totalTicks = CalculateTicks();
            var timeTaken = TimeSpan.FromTicks((long)(totalTicks * timestampToTicks));

            this.timingsContainer.Timings.Add(
                new(
                    this.queryPart,
                    timeTaken, 
                    result.Matches.Count,
                    this.DocumentFiltersApplied,
                    this.FieldFiltersApplied));

            pool.Return(this);

            return result;
        }
    }
}

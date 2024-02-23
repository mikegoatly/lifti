using Lifti.Querying.QueryParts;
using System;
using System.Diagnostics;

namespace Lifti.Querying
{
    internal class QueryPartTimer
    {
        private static readonly SharedPool<QueryPartTimer> queryPartTimerPool = new(static () => new QueryPartTimer(), static (o) => { }, 10);
        private static readonly double timestampToTicks = TimeSpan.TicksPerSecond / (double)Stopwatch.Frequency;
        private ExecutionTimings timingsContainer = null!;
        private IQueryPart queryPart = null!;
        private long startTimestamp;

        public static readonly QueryPartTimer NullTimer = new NullQueryPartTimer();

        public int? DocumentFiltersApplied { get; private set; }
        public int? FieldFiltersApplied { get; private set; }

        protected QueryPartTimer()
        {
        }

        public static QueryPartTimer StartNew(ExecutionTimings parent, IQueryPart queryPart, QueryContext currentContext)
        {
            return Start(parent, queryPart, currentContext);
        }

        private static QueryPartTimer Start(ExecutionTimings parent, IQueryPart queryPart, QueryContext currentContext)
        {
            var timer = queryPartTimerPool.Take();
            timer.timingsContainer = parent;
            timer.DocumentFiltersApplied = currentContext.FilterToDocumentIds?.Count ?? null;
            timer.FieldFiltersApplied = currentContext.FilterToFieldId.HasValue ? 1 : null;
            timer.queryPart = queryPart;
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

        public virtual IntermediateQueryResult Complete(IntermediateQueryResult result)
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

            queryPartTimerPool.Return(this);

            return result;
        }

        private sealed class NullQueryPartTimer : QueryPartTimer
        {
            public override IntermediateQueryResult Complete(IntermediateQueryResult result)
            {
                return result;
            }
        }
    }
}

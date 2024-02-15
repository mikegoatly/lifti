using Lifti.Querying.QueryParts;
using System;
using System.Collections.Generic;

namespace Lifti.Querying
{
    internal class ExecutionTimings
    {
        public static readonly ExecutionTimings NullTimings = new NullExecutionTimings();

        public virtual QueryPartTimer Start(IQueryPart queryPart, QueryContext currentContext)
        {
            return QueryPartTimer.StartNew(this, queryPart, currentContext);
        }

        internal List<QueryPartExecutionDetails> Timings { get; } = [];

        private class NullExecutionTimings : ExecutionTimings
        {
            public override QueryPartTimer Start(IQueryPart queryPart, QueryContext currentContext)
            {
                return QueryPartTimer.NullTimer;
            }
        }
    }
}

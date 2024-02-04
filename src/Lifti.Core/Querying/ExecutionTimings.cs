using Lifti.Querying.QueryParts;
using System;
using System.Collections.Generic;

namespace Lifti.Querying
{
    internal class ExecutionTimings
    {
        public QueryPartTimer Start(IQueryPart queryPart, QueryContext currentContext)
        {
            return QueryPartTimer.StartNew(this, queryPart, currentContext);
        }

        internal List<QueryPartExecutionDetails> Timings { get; } = [];
    }
}

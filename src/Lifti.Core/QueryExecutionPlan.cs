using Lifti.Querying;
using Lifti.Querying.QueryParts;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Lifti
{
    /// <summary>
    /// The execution plan for a query. Can be used to determine the order and timing of the query parts,
    /// along with information such as the number of documents returned at each stage.
    /// </summary>
    public class QueryExecutionPlan
    {
        internal QueryExecutionPlan(int resultCount, ExecutionTimings executionTimings)
        {
            if (executionTimings.Timings.Count == 0)
            {
                this.Root = new QueryExecutionPlanNode(1, resultCount, currentPart: null, null);
            }

            // We're going to loop through the timings. Each time the parent node changes, we'll create
            // a new node and add it to the graph.
            int executionOrder = 1;
            Stack<QueryExecutionPlanNode> children = new(2);

            (QueryExecutionPlanNode left, QueryExecutionPlanNode right)? PopChildren()
            {
                // Because we're using a stack, the right node is first, followed by the left
                var right = children.Pop();
                return (children.Pop(), right);
            }

            foreach (var timing in executionTimings.Timings)
            {
                if (timing.QueryPart is ScoreBoostedQueryPart)
                {
                    // These can never have children in their own right
                    children.Push(new QueryExecutionPlanNode(executionOrder++, timing.DocumentCount, timing, null));
                }
                else
                {
                    var childBranches = children.Count >= 2 ? PopChildren() : null;
                    children.Push(new QueryExecutionPlanNode(executionOrder++, timing.DocumentCount, timing, childBranches));
                }
            }

            // Create the final root node
            if (children.Count == 0)
            {
                this.Root = new QueryExecutionPlanNode(1, resultCount, null, null);
            }
            else if (children.Count == 1)
            {
                this.Root = children.Pop();
            }
            else if (children.Count == 2)
            {
                this.Root = new QueryExecutionPlanNode(executionOrder++, resultCount, null, PopChildren());
            }
            else
            {
                throw new InvalidOperationException("Too many children encountered");
            }
        }

        /// <summary>
        /// Gets the root the execution plan - this is the the ultimate results of the query. Children of this node
        /// contribute towards the final result.
        /// </summary>
        public QueryExecutionPlanNode Root { get; }
    }

    /// <summary>
    /// The type of node in the <see cref="QueryExecutionPlan"/>.
    /// </summary>
    public enum QueryExecutionPlanNodeKind
    {
        /// <summary>
        /// The node is of an unknown type.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// The node represents a query part.
        /// </summary>
        QueryPart = 1,

        /// <summary>
        /// The node unions the results from both sides into one set.
        /// </summary>
        Union = 2,

        /// <summary>
        /// The node intersects the results from its children, only returning results for documents
        /// that are present on both sides of the intersection.
        /// </summary>
        Intersect = 3,

        /// <summary>
        /// The node intersects the results from its children, only returning results for documents
        /// that are present on both sides of the intersection, and are within the specified distance
        /// of each other. Intersected matches are combined into a single logical match, maintaining
        /// their relationship for subsequent positional intersect operations.
        /// </summary>
        PositionalIntersect = 4,

        /// <summary>
        /// The node intersects the results from its children, only returning results for documents
        /// that are present on both sides of the intersection, and are within the specified distance
        /// of each other. Intersected matches are combined into a single logical match, maintaining
        /// their relationship for subsequent positional intersect operations.
        /// </summary>
        [Obsolete("No longer used. PositionalIntersect should always be used - CompositePositionalIntersect was technically no different from that.")]
        CompositePositionalIntersect = 5,

        /// <summary>
        /// The node intersects the results from its children, only returning results for documents
        /// when the positions of the matched tokens on the left are preceding the tokens on the right.
        /// </summary>
        PrecedingIntersect = 6,

        /// <summary>
        /// No execution plan was recorded during execution - this node is a placeholder for the final results.
        /// </summary>
        ResultsOnly = 7,
    }

    /// <summary>
    /// A node in the <see cref="QueryExecutionPlan"/>.
    /// </summary>
    public class QueryExecutionPlanNode
    {
        internal QueryExecutionPlanNode(
            int executionOrder,
            int resultCount,
            QueryPartExecutionDetails? currentPart,
            (QueryExecutionPlanNode left, QueryExecutionPlanNode right)? children)
        {
            this.ExecutionOrder = executionOrder;
            this.ResultingDocumentCount = resultCount;

            if (currentPart != null)
            {
                this.ExclusiveTiming = currentPart.ExecutionTime;

                this.DocumentFiltersApplied = currentPart.DocumentFiltersApplied;
                this.FieldFiltersApplied = currentPart.FieldFiltersApplied;

                (this.Kind, this.Text, this.Weighting) = DeriveQueryPartSpecificInformation(currentPart.QueryPart);
            }
            else
            {
                this.Text = "Results";
                this.ExclusiveTiming = TimeSpan.Zero;
                this.Kind = QueryExecutionPlanNodeKind.ResultsOnly;
            }

            this.InclusiveTiming = this.ExclusiveTiming;
            if (children is (var left, var right))
            {
                this.InclusiveTiming += left.InclusiveTiming + right.InclusiveTiming;
            }

            this.Children = children;
        }

        /// <summary>
        /// Used for testing.
        /// </summary>
        internal QueryExecutionPlanNode(
            int executionOrder,
            QueryExecutionPlanNodeKind kind,
            int resultingDocumentCount,
            string? text = null,
            double? weighting = null,
            int? documentFiltersApplied = null,
            int? fieldFiltersApplied = null,
            (QueryExecutionPlanNode left, QueryExecutionPlanNode right)? children = null,
            TimeSpan inclusiveTiming = default,
            TimeSpan exclusiveTiming = default)
        {
            this.ExecutionOrder = executionOrder;
            this.Kind = kind;
            this.Text = text;
            this.InclusiveTiming = inclusiveTiming;
            this.ExclusiveTiming = exclusiveTiming;
            this.ResultingDocumentCount = resultingDocumentCount;
            this.Weighting = weighting;
            this.DocumentFiltersApplied = documentFiltersApplied;
            this.FieldFiltersApplied = fieldFiltersApplied;
            this.Children = children;
        }

        private static (QueryExecutionPlanNodeKind kind, string? text, double? weighting) DeriveQueryPartSpecificInformation(IQueryPart queryPart)
        {
            return queryPart switch
            {
                ScoreBoostedQueryPart sbqp => (QueryExecutionPlanNodeKind.QueryPart, sbqp.ToString(), sbqp.CalculatedWeighting),
                PrecedingNearQueryOperator pnq => (QueryExecutionPlanNodeKind.PositionalIntersect, $"~{pnq.Tolerance}>", null),
                NearQueryOperator nq => (QueryExecutionPlanNodeKind.PositionalIntersect, $"~{nq.Tolerance}", null),
                PrecedingQueryOperator => (QueryExecutionPlanNodeKind.PrecedingIntersect, $">", null),
                AdjacentWordsQueryOperator => (QueryExecutionPlanNodeKind.PositionalIntersect, $"~1>", null),
                AndQueryOperator => (QueryExecutionPlanNodeKind.Intersect, null, null),
                OrQueryOperator => (QueryExecutionPlanNodeKind.Union, null, null),
                _ => (QueryExecutionPlanNodeKind.Unknown, "UNKNOWN", null),
            };
        }

        /// <summary>
        /// Gets the position of this node in the execution order.
        /// </summary>
        public int ExecutionOrder { get; }

        /// <summary>
        /// Gets the kind of the node.
        /// </summary>
        public QueryExecutionPlanNodeKind Kind { get; }

        /// <summary>
        /// Gets the number of documents returned at this point of the query execution.
        /// </summary>
        public int ResultingDocumentCount { get; }

        /// <summary>
        /// Gets the weighting of the query part at this point of the query execution.
        /// </summary>
        public double? Weighting { get; }

        /// <summary>
        /// Gets the number of document filters applied at this point of the query execution.
        /// </summary>
        public int? DocumentFiltersApplied { get; }

        /// <summary>
        /// Gets the number of field filters applied at this point of the query execution.
        /// </summary>
        public int? FieldFiltersApplied { get; }

        /// <summary>
        /// The name of the node.
        /// </summary>
        public string? Text { get; }

        /// <summary>
        /// Gets the timings for this node, including the timings of any child nodes.
        /// </summary>
        public TimeSpan InclusiveTiming { get; private set; }

        /// <summary>
        /// Gets the timings for this node, excluding the timings of any child nodes.
        /// </summary>
        public TimeSpan ExclusiveTiming { get; }

        /// <summary>
        /// Any child nodes of this node.
        /// </summary>
        public (QueryExecutionPlanNode left, QueryExecutionPlanNode right)? Children { get; }
    }
}

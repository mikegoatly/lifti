using FluentAssertions;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Lifti.Tests
{
    public class QueryExecutionPlanTests : IAsyncLifetime
    {
        private readonly FullTextIndex<int> index;

        public QueryExecutionPlanTests()
        {
            this.index = new FullTextIndexBuilder<int>()
                .Build();
        }

        public Task DisposeAsync()
        {
            return Task.CompletedTask;
        }

        public async Task InitializeAsync()
        {
            await index.AddAsync(1, "one two three four");
            await index.AddAsync(2, "two three four five");
            await index.AddAsync(3, "three four five six");
            await index.AddAsync(4, "four five six seven");
        }

        [Fact]
        public void ShouldReturnNestedHierarchyForPositionalIntersectQueries()
        {
            var plan = this.index.Search("\"two three four\"").GetExecutionPlan();

            plan.Root.Should().BeEquivalentTo(
                new QueryExecutionPlanNode(
                    5,
                    QueryExecutionPlanNodeKind.CompositePositionalIntersect,
                    resultingDocumentCount: 2,
                    "~1>",
                    children: (
                        new QueryExecutionPlanNode(
                            3,
                            QueryExecutionPlanNodeKind.CompositePositionalIntersect,
                            resultingDocumentCount: 2,
                            "~1>",
                            children: (
                                new QueryExecutionPlanNode(1, QueryExecutionPlanNodeKind.QueryPart, resultingDocumentCount: 2, "TWO"),
                                new QueryExecutionPlanNode(2, QueryExecutionPlanNodeKind.QueryPart, resultingDocumentCount: 2, "THREE", documentFiltersApplied: 2)
                            )),
                        new QueryExecutionPlanNode(4, QueryExecutionPlanNodeKind.QueryPart, resultingDocumentCount: 2, "FOUR", documentFiltersApplied: 2)
                    )),
                // Exclude the IncludingTiming and ExclusiveTiming properties from the comparison at all levels
                // of the object graph
                o => o.Excluding(m => m.Path.EndsWith("Timing")));
        }

        [Fact]
        public void TopLevelUnionShouldUnionBothTrees()
        {
            // One document matches "seven" and 2 documents match "two three four". A union of the two
            // should result in 3 documents because the documents are unique.
            var plan = this.index.Search("seven | (two three four)").GetExecutionPlan();

            AssertionOptions.FormattingOptions.MaxDepth = 10;

            plan.Root.Should().BeEquivalentTo(
                new QueryExecutionPlanNode(
                    executionOrder: 7,
                    QueryExecutionPlanNodeKind.Union,
                    resultingDocumentCount: 3,
                    children:
                    (
                        new QueryExecutionPlanNode(1, QueryExecutionPlanNodeKind.QueryPart, resultingDocumentCount: 1, "SEVEN"),
                        new QueryExecutionPlanNode(
                            executionOrder: 6,
                            QueryExecutionPlanNodeKind.Intersect,
                            resultingDocumentCount: 2,
                            children:
                            (
                                new QueryExecutionPlanNode(2, QueryExecutionPlanNodeKind.QueryPart, resultingDocumentCount: 2, "TWO", weighting: 0.5D),
                                new QueryExecutionPlanNode(
                                    executionOrder: 5,
                                    QueryExecutionPlanNodeKind.Intersect,
                                    resultingDocumentCount: 2,
                                    documentFiltersApplied: 2,
                                    children:
                                    (
                                        new QueryExecutionPlanNode(3, QueryExecutionPlanNodeKind.QueryPart, resultingDocumentCount: 2, "THREE", documentFiltersApplied: 2, weighting: 0.75D),
                                        new QueryExecutionPlanNode(4, QueryExecutionPlanNodeKind.QueryPart, resultingDocumentCount: 2, "FOUR", documentFiltersApplied: 2, weighting: 1D)
                                    ))
                            ))
                    )),
                    // Exclude the IncludingTiming and ExclusiveTiming properties from the comparison at all levels
                    // of the object graph
                    o => o.Excluding(m => m.Path.EndsWith("Timing")));
        }
    }
}

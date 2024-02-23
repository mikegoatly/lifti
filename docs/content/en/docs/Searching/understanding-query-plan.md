---
title: "Understanding Query Execution Plans"
linkTitle: "Understanding query plans"
weight: 8
description: >
  LIFTI's Query Execution Plan provides detailed insights into the execution of a query, including the order, timing, and the structure of the query parts. 
---

## Overview

The `QueryExecutionPlan` class represents the execution strategy for a given query. It includes information about the sequence of operations, their timings, and the number of documents affected at each stage of the query.

## Properties

- **Root**: The root node of the execution plan, representing the final result of the query. The entire execution plan can be navigated from this node, providing a hierarchical view of the query execution process.

## Nodes and Kinds

The execution plan is structured as a tree of `QueryExecutionPlanNode` instances, each representing a part of the query execution process. Nodes are categorized by their `QueryExecutionPlanNodeKind`, which describes the type of operation:

- **Unknown**: The operation type is not recognized.
- **QueryPart**: Represents a discrete part of the query.
- **Union**: Combines results from both child nodes into one set.
- **Intersect**: Returns documents present in both child nodes.
- **PositionalIntersect**: Like `Intersect`, but only for documents where matched tokens are within a specified distance.
- **CompositePositionalIntersect**: A specialized form of `PositionalIntersect` that maintains relational information for subsequent operations.
- **PrecedingIntersect**: Returns documents where tokens in the left node precede those in the right node.
- **ResultsOnly**: A placeholder node representing the final query results without a specific operation.

## Node Properties

Each `QueryExecutionPlanNode` includes details about its part of the query execution:

- **ExecutionOrder**: The sequence number of the node within the execution plan.
- **Kind**: The type of operation (as outlined above).
- **ResultingDocumentCount**: The number of documents returned by this part of the query.
- **Weighting**: The weighting score calculated for this part of the query.
- **DocumentFiltersApplied**: The number of document filters applied at this stage.
- **FieldFiltersApplied**: The number of field filters applied.
- **Text**: A textual representation of the node, providing insight into the specific operation or query part.
- **InclusiveTiming**: The total time taken by this node and its children.
- **ExclusiveTiming**: The time taken by this node, excluding its children.
- **Children**: Child nodes of this node, representing subsequent operations in the query execution process.

## Usage Example

To analyze a query execution plan, start with the `Root` node and explore its properties and children. This allows you to trace the execution path, understand the impact of each operation, and identify potential areas for optimization.

```csharp
// Assume `results` is obtained from a LIFTI search operation
var executionPlan = results.GetExecutionPlan();

// Accessing the root node
var rootNode = executionPlan.Root;

// Example: iterating through the node hierarchy
void PrintNodeDetails(QueryExecutionPlanNode node, string indent = "")
{
    Console.WriteLine($"{indent}Node: {node.Text}, Kind: {node.Kind}, Docs: {node.ResultingDocumentCount}, Timing: {node.InclusiveTiming.TotalMilliseconds} ms");
    if (node.Children.HasValue)
    {
        PrintNodeDetails(node.Children.Value.left, indent + "  ");
        PrintNodeDetails(node.Children.Value.right, indent + "  ");
    }
}

PrintNodeDetails(rootNode);
```

This documentation provides a foundation for developers to leverage the `QueryExecutionPlan` in LIFTI, enabling detailed analysis and optimization of full-text search queries.

The [Blazor sample](../../../blazor-sample/) provides a way to visualize the query execution plan:

![Visualization of a query execution plan](../../../images/query-execution-plan.png)
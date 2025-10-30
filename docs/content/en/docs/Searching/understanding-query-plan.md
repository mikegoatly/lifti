---
title: "Understanding Query Execution Plans"
linkTitle: "Understanding query plans"
weight: 8
description: >
  LIFTI's Query Execution Plan provides detailed insights into the execution of a query, including the order, timing, and the structure of the query parts. 
---

## Overview

The `QueryExecutionPlan` class represents the execution strategy for a given query. It includes information about the sequence of operations, their timings, and the number of documents affected at each stage of the query.

## Query execution plan nodes

### Node properties

The execution plan is structured as a tree of `QueryExecutionPlanNode` instances, each representing a part of the query execution process. Each `QueryExecutionPlanNode` includes details about its part of the query execution:

- **ExecutionOrder**: The sequence number of the node within the execution plan.
- **Kind**: The type of operation performed at the node - see the examples below.
- **ResultingDocumentCount**: The number of documents returned by this part of the query.
- **Weighting**: The weighting score calculated for this part of the query. If the execution of the query didn't necessitate the score to be calculated, this will be null.
- **DocumentFiltersApplied**: The number of document filters applied at this stage. This will be non-null if the results from a preceding evaluation can be used to pre-filter
the results at this node, mitigating the need for scoring matches that will just be discarded in a subsequent node.
- **FieldFiltersApplied**: The number of field filters applied.
- **Text**: A textual representation of the node, providing insight into the specific operation or query part.
- **InclusiveTiming**: The total time taken by this node and its children.
- **ExclusiveTiming**: The time taken by this node, excluding its children.
- **Children**: Child nodes of this node, representing subsequent operations in the query execution process.

### Node kinds

#### QueryPart

Represents a textual query that was evaluated against the index. The text of the node will contain the query.

![8 documents returned for the query "?2,2?CRITICAL" in 16.4ms](../../../images//QueryPlanImages/QueryPart.png)

#### Union

Typically introduced by an OR (|) operator. Results from the child nodes are combined into one set. Where documents are present in both nodes, their field matches are combined.

![Union between two searches, "CRITICAL" returning 5 documents and "HIT" returning 1. Union results in 6 documents total](../../../images//QueryPlanImages/Union.png)

#### Intersect

Typically introduced by an AND (&) operator. Only document results that are present in both child nodes are returned. Field matches for the intersected documents are combined.

![Intersection between two searches, "ANIMAL" (3 documents) and "SPECIES" (1 document). Intersection results in only 1 matching document](../../../images//QueryPlanImages/Intersect.png)

> Note the 3 document filters applied to the second search (SPECIES) - these are the 3 documents that were returned from the first search (ANIMAL). Without those document filters applied, searching for SPECIES would have returned many more documents.

#### Except

Introduced by the AND-NOT (&!) operator (LIFTI v7+). Performs a difference operation between two sets of results, returning only documents that exist in the left node but not in the right node. Like `Intersect`, document filters are applied to optimize performance by only evaluating the right side for documents that matched the left side.

#### PositionalIntersect

Introduced by the near (~), preceding near (~>) and adjacent words ("") operators. Like `Intersect`, but the locations of the matched tokens are taken into 
consideration and combined into a composite matched location which allows for the results of multiple positional matches to be combined in sequence.
Intersected documents that have no appropriate tokens matching are filtered out.

![alt text](../../../images//QueryPlanImages/PositionalIntersect.png)

#### PrecedingIntersect

Introduced by the preceding (>) operator. Intersects matched documents but only where the matched tokens in the left node precede those in the right node. 
Intersected documents that have no appropriate tokens matching are filtered out.

![alt text](../../../images//QueryPlanImages/PrecedingIntersect.png)

#### ResultsOnly

A placeholder node representing the final query results without a specific operation. This will only ever appear if the index 
was queried without the `QueryExecutionOptions.IncludeExecutionPlan` option specified.

## Usage Example

To analyze a query execution plan, start with the `Root` node and explore its properties and children. This allows you to trace the execution path, understand the impact of each operation, and identify potential areas for optimization.

<iframe width="100%" height="600" src="https://dotnetfiddle.net/Widget/slhurS" frameborder="0"></iframe>

The [Blazor sample](../../../blazor-sample/) provides a way to visualize the query execution plan:

![Visualization of a query execution plan](../../../images/query-execution-plan.png)
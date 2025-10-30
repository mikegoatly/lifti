---
title: "Query execution"
linkTitle: "Query execution"
weight: 2
description: >
  LIFTI attempts to optimize the order that query parts are executed to make queries as efficient as possible.
---

LIFTI's query execution logic employs a strategic approach to optimize query performance, prioritizing the execution of query parts based on their calculated weights. 
This weight represents the relative cost and effectiveness of executing a particular query part in relation to the overall document set. By assigning these weights, 
LIFTI aims to minimize the number of documents involved early in the query execution, thereby reducing the computational load and improving efficiency.

### Scoring mechanism overview

1. **General Principle**: Each query part is assigned a "weight" based on its expected execution cost and the number of documents it is likely to involve. 
The objective is to execute less costly parts first, reducing the document set size early in the process.

2. **Weighting Calculations for Different Query Parts**:
    - **ExactWordQueryPart**: Uses inverse document count. Weight = `Matching Document Count / Total Document Count`. Lower weights will result for words bringing in fewer documents.
    - **WildcardQueryPart**: A complex calculation involving the count of text matches, multi-character, and single-character wildcard matches, with different multipliers. Weight is reduced if the first query part is a text match, and increased it it has a leading multi-character match.
    - **FuzzyMatchQueryPart**: Based on the ExactWordQueryPart's weighting, adding a factor for the number of edits allowed. Weight increases with more permissible edits.
    - **AdjacentWordsQueryPart**: Assumes the score of the first part of the query, adjusted by the inverse of the number of parts.
    - **Intersection Query Parts** (AndQueryPart, PrecedingNearQueryPart, NearQueryPart, PrecedingQueryPart): Promotes parts based on the lowest scoring part of the intersection, encouraging execution of the cheapest part first.
    - **AndNotQueryOperator**: Uses the minimum of the left and right scores, similar to other intersection operations. Weight = `Min(Left Score, Right Score)`.
    - **OrQueryOperator**: A union operation always requires both sides to be evaluated, so weight = `Left Score + Right Score`.
    - **FieldFilterQueryOperator**: Applies additional filtering, thus promoted. Weight = `Child Part Score * 0.5`.
    - **BracketedQueryPart**: Reflects the score of the child part.

### Execution strategy

The execution order is determined by these weightings, with lower weights prioritized. This approach ensures that the query processor can efficiently 
filter and score documents, avoiding unnecessary computations on documents that would later be excluded. This system is especially beneficial in queries with multiple parts, 
where early reduction in document set size can lead to significant performance improvements.

### Execution plans

To get more insight into how a query is executed, including the weighting scores that were calculated you can get LIFTI to generate the actual execution plan for a query.

``` csharp
// Execute the query, indicating that timings and other details should be included
var results = index.Search("find something", QueryExecutionOptions.IncludeExecutionPlan);

// Calculate and return the execution plan details
var actualExecutionPlan = results.GetExecutionPlan();
```

See [Understanding LIFTI query execution plans](./understanding-query-plan) for more details.
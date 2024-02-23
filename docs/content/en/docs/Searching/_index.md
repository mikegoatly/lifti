---
title: "Searching"
linkTitle: "Searching"
weight: 5
description: >
  How to search against a LIFTI index
---

There are two ways to query a `FullTextIndex`, searching with a query string and executing a manually constructed `Query`.

## Searching with text

``` csharp
index.Search("find something");
```

This approach uses the `IQueryParser` implementation configured for the index to parse the query text into a `Query` object, which is then executed.

The default `IQueryParser` that is configured against an index parses the [LIFTI query syntax](./lifti-query-syntax), which is a fairly flexible way of expressing a query. If you want to swap out this parser with a custom implementation, you can do so when building the index using [`WithQueryParser`](../index-construction/withqueryparser) option.

## Searching with a `Query`

You can manually construct a `Query` using any combination of `IQueryPart`s, but take care to normalize any text to match in the same way that it has been indexed. You can do this using either the `IIndexTokenizer` for the index, or if specific tokenization rules have been [configured for a field](../index-construction/withobjecttokenization), then the tokenizer for that field:

``` csharp
var tokenizer = index.DefaultTokenizer;
var query = new Query(
    new AndQueryPart(
        new ExactWordQueryPart(tokenizer.Normalize("hello")), 
        new ExactWordQueryPart(tokenizer.Normalize("there"))));
```

## Obtaining query execution plans

By specifying `QueryExecutionOptions.IncludeExecutionPlan` when searching, LIFTI will collect timing information during the execution of the various query parts:

``` csharp
index.Search("find something", QueryExecutionOptions.IncludeExecutionPlan);
```

This enables `ISearchResults.GetExecutionPlan` to return the actual execution plan for the query. If `QueryExecutionOptions.IncludeExecutionPlan` is not specified, `GetExecutionPlan` returns a placeholder execution plan.

See [Understanding LIFTI query execution plans](./understanding-query-plan) for more details.
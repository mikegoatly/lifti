---
title: "Using the simple query parser"
linkTitle: "Using the simple query parser"
weight: 6
description: >
  You can use a simple query parser when you don't want queries to make use of the full LIFTI query syntax.
---

## Configuring the `SimpleQueryParser`

`FullTextIndexBuilder<TKey> WithSimpleQueryParser()`

`FullTextIndexBuilder<TKey> WithSimpleQueryParser(Func<QueryParserBuilder, QueryParserBuilder> optionsBuilder)`

If you don't want to be querying against the index using the full [LIFTI query syntax](../../searching/lifti-query-syntax) then you can use this method to query using simplified queries. 

You can optionally configure the query parser using the `QueryParserBuilder`, as you can with `WithQueryParser`: [QueryParserBuilder options](../withqueryparser/#queryparserbuilder-options).

### Example usage

``` csharp
var index = new FullTextIndexBuilder<int>()
    .WithSimpleQueryParser()
    .Build();

var index = new FullTextIndexBuilder<int>()
    .WithSimpleQueryParser(o => o.AssumeFuzzySearch())
    .Build();
```
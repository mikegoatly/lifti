---
title: "WithQueryParser"
linkTitle: "WithQueryParser"
weight: 5
description: >
  Prescribes how the QueryParser implementation should be configured for the index.
---

## Providing a complete `IQueryParser` implementation

`FullTextIndexBuilder<TKey> WithQueryParser(IQueryParser queryParser)`

Allows you to provide your own implementation of `IQueryParser` capable of parsing text into an `IQuery`.

## Configuring the default LIFTI `QueryParser`

`FullTextIndexBuilder<TKey> WithQueryParser(Func<QueryParserBuilder, QueryParserBuilder> optionsBuilder)`

By default LIFTI parses query text using the [LIFTI query syntax](../searching). The behavior of the parser can
be tweaked using this overload.

`QueryParserBuilder.AssumeFuzzySearchTerms()`
When used, uses fuzzy matching for any parsed search terms that don't contain
wildcard operators, i.e. you don't need to prefix search terms with `?`.

`QueryParserBuilder.WithQueryParserFactory(Func<QueryParserOptions, IQueryParser>)`
Given a `QueryParserOptions`, creates the implementation of `IQueryParser`. You can use this to provide a
custom query parsing strategy.

### Example usage

``` csharp
var index = new FullTextIndexBuilder<int>()
    .WithQueryParser(o => o.AssumeFuzzySearch())
    .Build();
```
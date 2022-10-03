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

To see an example of a custom `IQueryParser` implementation, see this [blog post](https://www.goatly.net/post/custom-query-parsers-with-lifti/).

## Configuring the default LIFTI `QueryParser`

`FullTextIndexBuilder<TKey> WithQueryParser(Func<QueryParserBuilder, QueryParserBuilder> optionsBuilder)`

By default LIFTI parses query text using the [LIFTI query syntax](../../searching/lifti-query-syntax). The behavior of the parser can
be tweaked using this overload.

### QueryParserBuilder options

`QueryParserBuilder.AssumeFuzzySearchTerms()`
When used, uses fuzzy matching for any parsed search terms that don't contain
wildcard operators, i.e. you don't need to prefix search terms with `?`.

`QueryParserBuilder.WithFuzzySearchDefaults(ushort maxEditDistance, ushort maxSequentialEdits)`
Configures the default parameters for a fuzzy search when not provided explicitly as part of the query. This overload
provides static values to use for the maximum edit distance and maximum sequential edits for a fuzzy search.

`QueryParserBuilder.WithFuzzySearchDefaults(Func<int, ushort> maxEditDistance, Func<int, ushort> maxSequentialEdits)`
Configures the default parameters for a fuzzy search when not provided explicitly as part of the query. This overload
allows for the maximum edit distance and maximum sequential edits for a fuzzy search to be calculated from the length 
of a search term.

`QueryParserBuilder.WithQueryParserFactory(Func<QueryParserOptions, IQueryParser>)`
Given a `QueryParserOptions`, creates the implementation of `IQueryParser`. You can use this to provide a
custom query parsing strategy.

### Example usage

``` csharp
var index = new FullTextIndexBuilder<int>()
    .WithQueryParser(o => o.AssumeFuzzySearch())
    .Build();
```
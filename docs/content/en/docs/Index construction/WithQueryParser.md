---
title: "WithQueryParser"
linkTitle: "WithQueryParser"
weight: 5
description: >
  Prescribes how the QueryParser implementation should be configured for the index.
---

## Configuring the default LIFTI `QueryParser`

`FullTextIndexBuilder<TKey> WithQueryParser(Func<QueryParserBuilder, QueryParserBuilder> optionsBuilder)`

By default LIFTI parses query text using the [LIFTI query syntax](../../searching/lifti-query-syntax). The behavior of the parser can
be tweaked using this overload.

## Providing a complete `IQueryParser` implementation

`FullTextIndexBuilder<TKey> WithQueryParser(IQueryParser queryParser)`

Allows you to provide your own implementation of `IQueryParser` capable of parsing text into an `IQuery`.

To see an example of a custom `IQueryParser` implementation, see this [blog post](https://www.goatly.net/post/custom-query-parsers-with-lifti/).

### QueryParserBuilder options

#### WithDefaultJoiningOperator

`QueryParserBuilder.WithDefaultJoiningOperator(QueryTermJoinOperatorKind joiningOperator)`
The default joining operator for queries is `and`. This means that without explicitly adding in an `&` or `|` to a LIFTI query,
`&` will be assumed, and all search terms must match in a document. This method allows you to choose between `and` and `or` as
the default joining operator.

#### AssumeFuzzySearchTerms

`QueryParserBuilder.AssumeFuzzySearchTerms()`
When used, uses fuzzy matching for any parsed search terms that don't contain
wildcard operators, i.e. you don't need to prefix search terms with `?`.

#### WithFuzzySearchDefaults

`QueryParserBuilder.WithFuzzySearchDefaults(ushort maxEditDistance, ushort maxSequentialEdits)`
Configures the default parameters for a fuzzy search when not provided explicitly as part of the query. This overload
provides static values to use for the maximum edit distance and maximum sequential edits for a fuzzy search.

`QueryParserBuilder.WithFuzzySearchDefaults(Func<int, ushort> maxEditDistance, Func<int, ushort> maxSequentialEdits)`
Configures the default parameters for a fuzzy search when not provided explicitly as part of the query. This overload
allows for the maximum edit distance and maximum sequential edits for a fuzzy search to be calculated from the length 
of a search term.

If **either** of these functions returns zero, then fuzzy matching will be disabled for the search term. 

By default `maxEditDistance` is calculated as `termLength / 3`, which means that fuzzy matching will be disabled for any search term less than 2 characters. The default for `maxSequentialEdits` is `Max(1, termLength / 4)`, which means that no matter
how short the search term, at least one sequential edit is allowed.

See [Fuzzy Matching](../../searching/fuzzy-matching).

#### WithQueryParserFactory

`QueryParserBuilder.WithQueryParserFactory(Func<QueryParserOptions, IQueryParser>)`
Given a `QueryParserOptions`, creates the implementation of `IQueryParser`. You can use this to provide a
custom query parsing strategy.

### Example usage

``` csharp
var index = new FullTextIndexBuilder<int>()
    .WithQueryParser(o => o.AssumeFuzzySearch())
    .Build();
```
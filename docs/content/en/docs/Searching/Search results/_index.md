---
title: Search Results
linkTitle: "Search Results"
date: 2020-07-14
description: "`FullTextIndex<T>.Search` returns `ISearchResults<T>`, which implements `IEnumerable<SearchResult<T>>` and provides other utilities for processing the matched search locations further."
---

## Search result order

Search results are returned sorted according to the total document score, in descending order. See [scoring](./scoring) for more information.

## `SearchResult&lt;TKey&gt;`

### `TKey Key { get; }`

The key for the document that matched the search criteria.

### `IReadOnlyList&lt;FieldSearchResult&gt; FieldMatches { get; }`

The fields that were matched for the document. Each of these is scored independently and provides detailed information
about the location of the words that were matched.

### `double Score { get; }`

The overall score for this match. This is a sum of the scores for this instance's FieldMatches.

## `FieldSearchResult`

### `string FoundIn { get; }`

The name of the field that the search results were found in. This will be one of the field names configured when the index was built, or `Unspecified` if no fields were configured.

### `double Score { get; }`

The score for this particular field.

### `IReadOnlyList&lt;TokenLocation&gt; Locations { get; }`

The `TokenLocation` instances for the locations of the matched tokens in the field.

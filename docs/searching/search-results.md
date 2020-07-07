---
title: LIFTI Search Results
permalink: /searching/query-syntax/
---

Results from `FullTextIndex<T>.Search` are an enumeration of `SearchResult<T>`

## SearchResult&lt;T&gt;

### T Item { get; }

The item that matched the search criteria.

### IReadOnlyList<FieldSearchResult> FieldMatches { get; }

The fields that were matched for the item. Each of these is scored independently and provides detailed information
about the location of the words that were matched.

### double Score { get; }

The overall score for this match. This is a sum of the scores for this instance's FieldMatches.

## FieldSearchResult

### string FoundIn { get; }

The name of the field that the search results were found in. This will be one of the field names configured when the index was built, or `Unspecified` if no fields were configured.

### double Score { get; }

The score for this particular field.

### IReadOnlyList<WordLocation> Locations { get; }

The `WordLocation` instances for the locations of the matched words in the field.

## Scoring

LIFTI uses a version of the [Okapi BM25](https://en.wikipedia.org/wiki/Okapi_BM25) algorithm to score search results. At the simplest level this means that search results will come back ordered by relevance.

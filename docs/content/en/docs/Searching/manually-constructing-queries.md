---
title: Manually Constructing Query Objects
linkTitle: "Manually Constructing Query Objects"
date: 2022-02-12
description:
    Instead of using a query parser to interpret your query, you can manually construct a `Query` object and execute it against the index.
---

You can manually construct a `Query` using any combination of `IQueryPart`s, but take care to normalize any text to match in the same way that it has been indexed. You can do this using either the `ITokenizer` for the index, or if specific tokenization rules have been [configured for a field](../index-construction/withobjecttokenization), then the tokenizer for that field:

``` csharp
var tokenizer = index.DefaultTokenizer;
var query = new Query(
    new AndQueryOperator(
        new ExactWordQueryPart(tokenizer.Normalize("hello")), 
        new ExactWordQueryPart(tokenizer.Normalize("there"))));
```

## Query Parts

`IQueryPart`s come in two flavors:

1. Operators - these contain other `IQueryParts`, combining the results that they return according to certain rules, e.g. `AndQueryPart` and `OrQueryPart`
2. Textual - these work the [`IIndexNavigator`](../using-the-index-navigator/) to query the index for matches appropriate to them, e.g. `ExactWordQueryPart`, `FuzzyMatchQueryPart`.

### Textual query parts

#### ExactWordQueryPart(string word)

Searches the index for words that exactly match `word`.

#### FuzzyMatchQueryPart(string word, ushort maxEditDistance = 4, ushort maxSequentialEdits = 1)

> `?` in LIFTI query syntax

Performs a [fuzzy match](../lifti-query-syntax#fuzzy-match-) against the index. 

* `maxEditDistance`: The maximum of edits allowed for any given match. The higher this value, the more divergent matches will be.
* `maxSequentialEdits` The maximum number of edits that are allowed to appear sequentially. By default this is 1, which forces matches to be more similar to the search criteria 

#### WildcardQueryPart(IReadOnlyList&lt;WildcardQueryFragment&gt; fragments)

> `*` and `%` in LIFTI query syntax

A `WildcardQueryPart` consists of multiple `WildcardQueryFragment`s that are processed sequentially to match tokens in the index. They can be constructed using the following methods:

* `WildcardQueryFragment.MultiCharacter()` - matches zero or more characters in the index.
* `WildcardQueryFragment.SingleCharacter()` - matches any single character in the index.
* `WildcardQueryFragment.CreateText(string text)` - exact matches a fragment of text at whatever point has been reached in the index.

For example:

``` csharp
var wildcard = new WildcardQueryPart(new[] { 
    WildcardQueryFragment.SingleCharacter(),
    WildcardQueryFragment.SingleCharacter(),
    WildcardQueryFragment.CreateText("d")
})
```

Would translate to the LIFTI query `%%d`, matching any words that start with any two letters followed by a `d`.

### Structural query parts

#### `AndQueryOperator(IQueryPart left, IQueryPart right)`

> `&` in LIFTI query syntax

Intersects the results of two query parts. In other words, only matches that appear on **both** sides will be returned.

#### `OrQueryOperator(IQueryPart left, IQueryPart right)`

> `|` in LIFTI query syntax

Unions the results of two query parts. In other words, a deduplicated set of matches on **both** sides will be returned.

#### `BracketedQueryPart(IQueryPart statement)`

> `(` and `)` in LIFTI query syntax

This can be used to group other query parts together, ensure they are executed in the right order.

#### `AdjacentWordsQueryPart(IReadOnlyList<IQueryPart> matches)`

> `"` in LIFTI query syntax

A query part requiring that a series of matches must appear in a document in sequence.

#### `FieldFilterQueryOperator(string fieldName, byte fieldId, IQueryPart statement)`

> `field=` in LIFTI query syntax

Restricts the resulting item matches to only those that include matching tokens in a specific field.

#### `NearQueryOperator(IQueryPart left, IQueryPart right, int tolerance = 5)`

> `~n` in LIFTI query syntax

Produces an intersection of two `IQueryPart`s, restricting an item's field matches such that the locations are close to one another. 

Items that result in no field matches are filtered out.

#### `PrecedingNearQueryOperator(IQueryPart left, IQueryPart right, int tolerance = 5)`

> `~n>` in LIFTI query syntax

Produces an intersection of two `IQueryPart`s, restricting an item's field matches such that the locations of the first appear before the locations of the second and within a specified tolerance. 

Items that result in no field matches are filtered out.

#### `PrecedingQueryOperator(IQueryPart left, IQueryPart right)`

> `>` in LIFTI query syntax

Produces an intersection of two `IQueryPart`s, restricting an item's field matches such that the locations of the first appear before the locations of the second. 

Items that result in no field matches are filtered out.
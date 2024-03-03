---
title: Manually Constructing Queries
linkTitle: "Manually Constructing Queries"
date: 2022-02-12
weight: 5
description:
    Instead of using a query parser to interpret your query, you can manually construct a `Query` object and execute it against the index.
---

There are two approaches you can take to constructing a `Query`, using the fluent builder syntax and manually creating the relevant query parts. 

The simplest is the fluent builder syntax as it takes care of automatically normalizing your search text to be in line with that expected in the index.

## Fluent query building

All fluent query building starts from the index:

```csharp
index.Query().ExactMatch("text");
```

You can immediately execute a built query:

```csharp
var searchResults = index.Query().ExactMatch("text").Execute();
```

Or just build an `IQuery` representing it:

```csharp
var query = index.Query().ExactMatch("text").Build();
```

You can exact match a search term, as in the above examples, fuzzy match or wildcard match:

```csharp
// Fuzzy match
var searchResults = index.Query().FuzzyMatch("text").Execute();

// Wildcard match, parsing from text
var searchResults = index.Query().WildcardMatch("te%*").Execute();

// Wildcard match, building explicitly
var searchResults = index.Query().WildcardMatch(w => w
    .Text("te")
    .SingleCharacter()
    .MultipleCharacters())
.Execute();
```

### Fluently combining search terms

Terms can be combined with and (`&`)/or (`|`):

```csharp
// Combine with And (&)
index.Query()
    .ExactMatch("west")
    .And.ExactMatch("wing")
    .Execute()

// Combine with Or (|)
index.Query()
    .ExactMatch("west")
    .Or.ExactMatch("wing")
    .Execute()
```

All other [LIFTI query operators](../lifti-query-syntax) are also supported:

```csharp
// Combine with Preceding (>)
index.Query()
    .ExactMatch("west")
    .Preceding.ExactMatch("wing")
    .Execute()

// Combine with Near (~)
index.Query()
    .ExactMatch("west")
    .Near(3).ExactMatch("wing")
    .Execute()

// Combine with Preceding Near (~>)
index.Query()
    .ExactMatch("west")
    .CloselyPreceding(3).ExactMatch("wing")
    .Execute()
```

### Adjacent words

You can build a series of search terms that must follow each other sequentially (i.e. "one two three"):

```csharp
index.Query()
    .Adjacent(a => a.ExactMatch("west").ExactMatch("wing"))
    .Execute()
```

Within an adjacent search you can also use fuzzy match or wildcard searches as well:

```csharp
index.Query()
    .Adjacent(a => a.FuzzyMatch("wst").WildcardMatch("wi*"))
    .Execute()
```

### Searching in a specific field

Like in the LIFTI query syntax to restrict a search to a specific field (e.g. `[fieldName]=text`) you can do the same fluently:

```csharp
index.Query()
    .InField("description", f => f.FuzzyMatch("west"))
    .Execute()
```

Multiple search terms in an `InField` call will automatically be placed in parenthesis:

```csharp
var query = index.Query()
    .InField("description", f => f.ExactMatch("west").Or.ExactMatch("wing"))
    .Build();

Console.WriteLine(query.ToString()); // [description]=(west | wing)
```

### Bracketing parts of the query

To logically group a section of a query together you can place them in a bracketed section:

```csharp
var query = index.Query()
    .ExactMatch("one")
    .Or
    .Bracketed(b => b
        .ExactMatch("two")
        .And.ExactMatch("three"))
    .Build();

Console.WriteLine(query.ToString()); // one | (two & three)
```

## Manual query construction

You can manually construct a `Query` using any combination of `IQueryPart`s, but take care to normalize any text to match in the same way that it has been indexed. You can do this using either the `IIndexTokenizer` for the index, or if specific tokenization rules have been [configured for a field](../index-construction/withobjecttokenization), then the tokenizer for that field:

``` csharp
var tokenizer = index.DefaultTokenizer;
var query = new Query(
    new AndQueryOperator(
        new ExactWordQueryPart(tokenizer.Normalize("hello")), 
        new ExactWordQueryPart(tokenizer.Normalize("there"))));
```

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

Restricts the resulting document matches to only those that include matching tokens in a specific field.

#### `NearQueryOperator(IQueryPart left, IQueryPart right, int tolerance = 5)`

> `~n` in LIFTI query syntax

Produces an intersection of two `IQueryPart`s, restricting an document's field matches such that the locations are close to one another. 

Documents that result in no field matches are filtered out.

#### `PrecedingNearQueryOperator(IQueryPart left, IQueryPart right, int tolerance = 5)`

> `~n>` in LIFTI query syntax

Produces an intersection of two `IQueryPart`s, restricting an document's field matches such that the locations of the first appear before the locations of the second and within a specified tolerance. 

Documents that result in no field matches are filtered out.

#### `PrecedingQueryOperator(IQueryPart left, IQueryPart right)`

> `>` in LIFTI query syntax

Produces an intersection of two `IQueryPart`s, restricting an document's field matches such that the locations of the first appear before the locations of the second. 

Documents that result in no field matches are filtered out.
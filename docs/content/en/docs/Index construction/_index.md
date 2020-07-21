---
title: "Index Construction"
linkTitle: "Index Construction"
weight: 2
description: >
  Use a `FullTextIndexBuilder<TKey>` to configure the behavior of your index.
---

## Quick examples

Create an index with defaults:

* case insensitive
* accent insensitive
* no word stemming
* splitting only on punctuation

``` csharp
var index = new FullTextIndexBuilder<int>()
    .Build();
```

Enable word stemming:

``` csharp
var index = new FullTextIndexBuilder<int>()
    .WithDefaultTokenizationOptions(o => o.WithStemming())
    .Build();
```

With object indexing enabled for a `Customer` type and stemming only enabled for the `Notes` property:

``` csharp
var index = new FullTextIndexBuilder<int>()
    .WithObjectTokenization(o => o
      .WithId(c => c.CustomerId)
      .WithField("Name", c=>c.Name)
      .WithField("Notes", c=>c.Notes, fo => fo.WithStemming())
    )
    .Build();
```

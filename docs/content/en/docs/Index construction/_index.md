---
title: "Index Construction"
linkTitle: "Index Construction"
weight: 2
description: >
  Use a `FullTextIndexBuilder<TKey>` to configure the behavior of your index.
---

## `FullTextIndexBuilder<TKey>`

`FullTextIndexBuilder` requires a single generic type provided to it. 
This defines the type of the key that documents will be indexed against.

In simple cases this will just be a `string`, `Guid`, `Int32` or `UInt32`. 
Indexes can be built with other key types, including composite types, but special care needs 
to be made when using the binary serializer. See [Key Serialization](../serialization/key-serialization) 
for more information.

## Quick examples

Create an index with defaults:

* case insensitive
* accent insensitive
* full [LIFTI query syntax](../searching) for queries (Don't want this? See [WithSimpleQueryParser](withsimplequeryparser))
* no word stemming
* splitting only on punctuation, whitespace and control characters

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
    .WithObjectTokenization<Customer>(o => o
      .WithId(c => c.CustomerId)
      .WithField("Name", c=>c.Name)
      .WithField("Notes", c=>c.Notes, fo => fo.WithStemming())
    )
    .Build();
```

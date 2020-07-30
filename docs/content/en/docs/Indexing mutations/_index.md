---
title: "Indexing Data"
linkTitle: "Indexing Data"
weight: 3
description: >
  Once an index has been constructed, it can be populated using the various `AddAsync` methods.
---

## Indexing text

As shown in [this example](../Getting%20started), if all you have is a key and text to be indexed for it, you can just make use one of the `AddAsync` overloads that operate on `string`s - there is no need to construct any artificial objects in order to populate the index.

Each of these methods will use the default text extraction and tokenization configured for the index and the default field id, `IFullTextIndex.FieldLookup.DefaultField`.

### `Task AddAsync(TKey itemKey, string text)`

Adds an item to the index treating the single `string` as the complete document text.

### `Task AddAsync(TKey itemKey, IEnumerable<string> text)`

Adds an item to the index with multiple text fragments. Each fragment is considered to be fragments of the same text, i.e. the index and offset of tokens increments across the elements in the `IEnumerable<string>`.

It's worth noting that each fragment is processed independently, so an implicit word break exists between element. This means that `AddAsync(1, new[ "a", "b" ])` will index two words (`a` and `b`) whereas if the two strings had been naively concatenated together, only one (`ab`) would, as there was no word break between them.

## Indexing Objects

In order to index an object against the index it needs to have first been configured when the index was built, using [WithObjectTokenization](../index-construction/withobjecttokenization). This allows for multiple properties of a single object to be extracted as different fields of the same document.

See the [Indexing objects](../getting-started/indexing-objects) quick start for an example of this in action. 

### `Task AddAsync<TItem>(TItem item)`

Adds a single item to the index.

### `Task AddRangeAsync<TItem>(IEnumerable<TItem> items)`

Adds a set of items to the index in a single mutation. This is more efficient than making multiple calls to `AddAsync<TItem(TItem item)>` unless a batch mutation has already been manually started.

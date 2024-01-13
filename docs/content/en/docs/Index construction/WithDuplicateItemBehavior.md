---
title: "Managing duplicate keys"
linkTitle: "Managing duplicate keys"
weight: 5
description: >
  Configure how the index should behave when indexing an item that is already present in the index.
---

`FullTextIndexBuilder<TKey> WithDuplicateItemBehavior(DuplicateItemBehavior duplicateItemBehavior)`

* `DuplicateItemBehavior.ReplaceItem`: **Default** The item will first be removed from the index, then indexed
* `DuplicateItemBehavior.ThrowException`: An exception will be thrown. You can use this if you're not expecting items to be re-indexed and want some indication that your code isn't behaving correctly.

## Example usage

``` csharp
var index = new FullTextIndexBuilder<int>()
    .WithDuplicateItemBehavior(DuplicateItemBehavior.ThrowException)
    .Build();
```
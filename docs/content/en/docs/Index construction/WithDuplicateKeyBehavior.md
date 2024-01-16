---
title: "Managing duplicate keys"
linkTitle: "Managing duplicate keys"
weight: 5
description: >
  Configure how the index should behave when indexing an item that is already present in the index.
---

`FullTextIndexBuilder<TKey> WithDuplicateKeyBehavior(DuplicateKeyBehavior duplicateKeyBehavior)`

* `DuplicateKeyBehavior.Replace`: **Default** The document associated to the key will first be removed from the index, then indexed
* `DuplicateKeyBehavior.ThrowException`: An exception will be thrown. You can use this if you're not expecting keys to be re-indexed and want some indication that your code isn't behaving correctly.

## Example usage

``` csharp
var index = new FullTextIndexBuilder<int>()
    .WithDuplicateKeyBehavior(DuplicateKeyBehavior.ThrowException)
    .Build();
```
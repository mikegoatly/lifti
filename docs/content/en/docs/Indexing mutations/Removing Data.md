---
title: "Removing Data"
linkTitle: "Removing Data"
weight: 3
description: >
  Data can be removed from an index using the `RemoveAsync` method.
---

## RemoveAsync(TKey key)

Removes all indexed data for the given item key.

Removal is considered an index mutation in the same way that adding is, and batches of changes can be sped up by using the `BeginBatchChange` and `CommitBatchChangeAsync` methods.

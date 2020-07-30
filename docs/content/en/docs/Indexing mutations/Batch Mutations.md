---
title: "Batch Mutations"
linkTitle: "Batch Mutations"
weight: 2
description: >
  Each index mutation causes a new immutable snapshot to be generated. You can speed up the indexing process by calling `BeginBatchChange` before mutating the index and `CommitBatchChangeAsync` once all the mutations have been performed.
---

``` csharp
index.BeginBatchChange();
await index.AddAsync(1, "This is some text associated with A: fizz");
await index.AddAsync(2, "Some buzz text for B");
await index.AddAsync(3, "Text associated with C is both fizz and buzz");
await index.CommitBatchChangeAsync();
```

Only once `CommitBatchChangeAsync` has been called will the new data be available in the index for searching. If a search operation was to have been performed between any of the calls to `AddAsync` then the previous snapshot would have been used and it would be as if the new items had not been added yet.

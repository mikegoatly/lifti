---
title: "WithIndexModificationAction"
linkTitle: "WithIndexModificationAction"
weight: 10
description: >
  Registers an async action that needs to occur when mutations to the index are committed and a new snapshot is generated.
---

Every time an index is modified, either with a single document being added or a batch change being completed, a new immutable snapshot is created. 
This is part of LIFTI's thread safety mechanism.

You can hook into this process by registering an action using the `FullTextIndexBuilder<TKey>.WithIndexModificationAction` method.

This trivial example just logs to the console the number of items in the index whenever a new snapshot is created.

``` csharp
var index = new FullTextIndexBuilder<Guid>()
    .WithIndexModificationAction(async (idx) =>
    {
        Console.WriteLine($"Index now contains {idx.IdLookup.Count} items");
    })
    .Build();
```

You can also use this process to automatically serialize the index when modifications occur:

``` csharp
var serializer = new BinarySerializer<Guid>();

var index = new FullTextIndexBuilder<Guid>()
    .WithIndexModificationAction(async (idx) =>
    {
        using (var fileStream = await File.OpenWrite("myindex.dat"))
        {
            await serializer.SerializeAsync(idx, fileStream);
        }
    })
    .Build();
```

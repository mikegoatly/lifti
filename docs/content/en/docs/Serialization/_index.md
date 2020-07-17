---
title: "Serialization"
linkTitle: "Serialization"
weight: 6
description: >
  A LIFTI index can be serialized to a stream, and deserialized at a later date.
---

Given an index:

``` csharp
// Create a full text index with default settings
var index = new FullTextIndexBuilder<int>().Build();

// Index some sample data
await index.AddAsync(1, "This is some text associated with A: fizz");
await index.AddAsync(2, "Some buzz text for B");
await index.AddAsync(3, "Text associated with C is both fizz and buzz");

```

You can serialize it to a stream using the `Lifti.Serialization.Binary.BinarySerializer` class:

``` csharp
var serializer = new BinarySerializer<int>();
using var stream = new MemoryStream();

// Serialize the index
await serializer.SerializeAsync(index, stream, disposeStream: false);

// Reset the stream back to the beginning so we can deserialize from it
stream.Position = 0; 
```

After you have created a new index instance, you can use the `BinarySerializer` class again to bring its state in line with the serialized index:

``` csharp
// Deserialize the index into a new instance
var newIndex = new FullTextIndexBuilder<int>().Build();
await serializer.DeserializeAsync(newIndex, stream, disposeStream: false);

// Prove that the new index has the same contents
// Emits: 3 items contain text in the new index
var matches = newIndex.Search("text");
Console.WriteLine($"{matches.Count()} items contain text in the new index");
```

If you want to understand how the binary data is laid out, you can have a look at the [Serialization Format](../reference/serialization-format) reference page.

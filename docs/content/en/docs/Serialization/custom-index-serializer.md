---
title: "Custom Index Serializer"
linkTitle: "Custom Index Serializer"
weight: 2
description: >
  How to implement and wire up a custom index serializer (for example, a JSON-based serializer).
---

LIFTI ships with `BinarySerializer<TKey>`, which is the recommended default for most applications.

If you need a different storage format (for example JSON), you can create your own serializer by implementing `Lifti.Serialization.Binary.IIndexSerializer<TKey>` and then plugging it into your persistence layer.

## Why use a custom serializer?

A custom serializer can be useful when you want to:

- inspect serialized data in a human-readable format,
- interoperate with external systems,
- add custom metadata/versioning around snapshots,
- or enforce application-specific persistence rules.

## Interfaces involved

At a high level:

- `IIndexSerializer<TKey>` provides `SerializeAsync` and `DeserializeAsync` methods to round-trip an index or snapshot.
- Your persistence layer is responsible for choosing when/how to persist snapshots and for wiring up the serializer (and deserializer).

That means your custom serializer can focus on its own format, while the persistence layer handles storage concerns (file, database, blob store, etc.).

## Minimal JSON serializer example

Below is a minimal example that writes a snapshot as JSON text.

```csharp
using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Lifti;
using Lifti.Serialization.Binary;

public sealed class JsonIndexSerializer<TKey> : IIndexSerializer<TKey>
    where TKey : notnull
{
    public Task SerializeAsync(FullTextIndex<TKey> index, Stream stream, bool disposeStream = true, CancellationToken cancellationToken = default)
    {
        return SerializeAsync(index.CreateSnapshot(), stream, disposeStream, cancellationToken);
    }

    public async Task SerializeAsync(IIndexSnapshot<TKey> snapshot, Stream stream, bool disposeStream = true, CancellationToken cancellationToken = default)
    {
        try
        {
            var payload = new
            {
                Version = 1,
                Documents = snapshot.Metadata.GetIndexedDocuments().Select(d => new
                {
                    d.Key,
                    d.ObjectTypeId,
                    d.ScoreBoost
                })
            };

            await JsonSerializer.SerializeAsync(stream, payload, cancellationToken: cancellationToken);
        }
        finally
        {
            if (disposeStream)
            {
                await stream.DisposeAsync();
            }
        }
    }

    public Task DeserializeAsync(FullTextIndex<TKey> index, Stream stream, bool disposeStream = true, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Example only: implement deserialization for your chosen payload format.");
    }
}
```

> The exact members available on `IIndexSnapshot<TKey>` may vary by version. Adapt the projected payload accordingly.

## Using your serializer

The typical pattern is:

1. Build and populate an index as normal.
2. Create your custom serializer implementation.
3. Use it through your chosen serialization workflow.

```csharp
var index = new FullTextIndexBuilder<int>().Build();
await index.AddAsync(1, "hello world");

using var stream = File.Create("index.json");
var serializer = new JsonIndexSerializer<int>();
await serializer.SerializeAsync(index.CreateSnapshot(), stream);
```

## Pairing with custom deserialization

For a complete round-trip format, implement a matching deserializer and use both together in your persistence layer.

A good practice is to include a format version in your serialized payload (like `Version = 1`) so you can evolve the format safely over time.
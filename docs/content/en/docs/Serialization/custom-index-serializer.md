---
title: "Custom Index Serializer"
linkTitle: "Custom Index Serializer"
weight: 2
description: >
  How to implement and wire up a custom index serializer (for example, a JSON-based serializer).
---

LIFTI ships with `BinarySerializer<TKey>`, which is the recommended default for most applications.

If you need a different storage format (for example JSON), you can create your own serializer by implementing `IIndexSerializer<TKey>` and then plugging it into `IndexSerializerBase<TKey>`.

## Why use a custom serializer?

A custom serializer can be useful when you want to:

- inspect serialized data in a human-readable format,
- interoperate with external systems,
- add custom metadata/versioning around snapshots,
- or enforce application-specific persistence rules.

## Interfaces involved

At a high level:

- `IIndexSerializer<TKey>` performs the low-level write of an `IIndexSnapshot<TKey>`.
- `IndexSerializerBase<TKey>` orchestrates the standard serialize/deserialize flow and accepts custom serializer/deserializer implementations.

That means a custom serializer can focus on its own output format, while `IndexSerializerBase<TKey>` still handles the overall snapshot lifecycle.

## Minimal JSON serializer example

Below is a minimal example that writes a snapshot as JSON text.

```csharp
using System.Text.Json;
using Lifti.Serialization;

public sealed class JsonIndexSerializer<TKey> : IIndexSerializer<TKey>
    where TKey : notnull
{
    private readonly Stream output;

    public JsonIndexSerializer(Stream output)
    {
        this.output = output;
    }

    public async ValueTask WriteAsync(IIndexSnapshot<TKey> snapshot, CancellationToken cancellationToken = default)
    {
        // Example payload shape - tailor to your own compatibility/versioning needs
        var payload = new
        {
            Version = 1,
            Documents = snapshot.Documents.Select(d => new
            {
                d.Key,
                d.Fields
            })
        };

        await JsonSerializer.SerializeAsync(this.output, payload, cancellationToken: cancellationToken);
        await this.output.FlushAsync(cancellationToken);
    }

    public void Dispose()
    {
        // No-op for this example. Dispose owned resources here if needed.
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
using var serializer = new JsonIndexSerializer<int>(stream);

await serializer.WriteAsync(index.CreateSnapshot());
```

## Pairing with custom deserialization

For a complete round-trip format, implement a matching deserializer and use both together in your persistence layer.

A good practice is to include a format version in your serialized payload (like `Version = 1`) so you can evolve the format safely over time.
---
title: "Custom Index Serialization"
linkTitle: "Custom Index Serialization"
weight: 3
description: >
  Implement a custom index serializer when the built-in binary format is not the right storage format.
---

The built-in `Lifti.Serialization.Binary.BinarySerializer<TKey>` is the simplest option for most applications. It writes the complete index to a compact binary stream and supports custom document key types through [key serialization](../key-serialization).

If you need to store the index in another format, for example JSON, a database record, or an application-specific archive, derive from the lower-level serialization base classes:

* `Lifti.Serialization.IndexSerializerBase<TKey>` writes an `IIndexSnapshot<TKey>` to your format.
* `Lifti.Serialization.IndexDeserializerBase<TKey>` reads your format back into an empty `FullTextIndex<TKey>`.

These base classes take care of the overall serialization order and index restoration. Your implementation is responsible for writing and reading the fields, document metadata, and node hierarchy in a consistent format.

## Writing an index

Derive from `IndexSerializerBase<TKey>` and provide the output mechanism your format needs. The base class calls the override methods in this order:

1. `OnSerializationStart`
2. `WriteFieldsAsync`
3. `WriteIndexMetadataAsync`
4. `WriteNodesAsync`
5. `OnSerializationComplete`

For example, a custom serializer normally has this shape:

``` csharp
using Lifti;
using Lifti.Serialization;

public sealed class CustomIndexSerializer<TKey> : IndexSerializerBase<TKey>
    where TKey : notnull
{
    public ValueTask SerializeAsync(
        FullTextIndex<TKey> index,
        CancellationToken cancellationToken = default)
    {
        return this.WriteAsync(index.Snapshot, cancellationToken);
    }

    protected override ValueTask WriteFieldsAsync(
        IReadOnlyList<SerializedFieldInfo> fields,
        CancellationToken cancellationToken)
    {
        // Store each SerializedFieldInfo:
        // - FieldId
        // - Name
        // - Kind
        // - DynamicFieldReaderName, when Kind is FieldKind.Dynamic
        return default;
    }

    protected override ValueTask WriteIndexMetadataAsync(
        IIndexSnapshot<TKey> index,
        CancellationToken cancellationToken)
    {
        foreach (var document in index.Metadata.GetIndexedDocuments())
        {
            // Store the document id, key, DocumentStatistics,
            // ObjectTypeId, ScoringFreshnessDate, and ScoringMagnitude.
        }

        return default;
    }

    protected override ValueTask WriteNodesAsync(
        IndexNode rootNode,
        CancellationToken cancellationToken)
    {
        // Store the root node and every child node reachable through ChildNodes.
        return default;
    }
}
```

When writing nodes, store the data exposed by each `IndexNode`:

* `IntraNodeText`
* `ChildNodes.CharacterMap`
* `Matches.Enumerate()`

Each match contains the internal document id and one or more `IndexedToken` values. Each `IndexedToken` stores a field id and a list of `TokenLocation` values.

## Reading an index

Derive from `IndexDeserializerBase<TKey>` and read the same data back in the same logical order. The base class uses the field metadata to map serialized field ids onto the field ids in the current index definition before it restores the node hierarchy.

``` csharp
using Lifti;
using Lifti.Serialization;

public sealed class CustomIndexDeserializer : IndexDeserializerBase<int>
{
    public ValueTask DeserializeAsync(
        FullTextIndex<int> index,
        CancellationToken cancellationToken = default)
    {
        return this.ReadAsync(index, cancellationToken);
    }

    protected override ValueTask<SerializedFieldCollector> DeserializeKnownFieldsAsync(
        CancellationToken cancellationToken)
    {
        var fields = new SerializedFieldCollector();

        // Add one entry for each stored field.
        fields.Add(new SerializedFieldInfo(
            fieldId: 1,
            name: "Name",
            kind: FieldKind.Static,
            dynamicFieldReaderName: null));

        return new(fields);
    }

    protected override ValueTask<DocumentMetadataCollector<int>> DeserializeDocumentMetadataAsync(
        CancellationToken cancellationToken)
    {
        var documents = new DocumentMetadataCollector<int>();

        var statistics = new DocumentStatistics(
            new Dictionary<byte, FieldStatistics>
            {
                [1] = new FieldStatistics(tokenCount: 3, lastTokenIndex: 2)
            },
            totalTokenCount: 3);

        // Use ForLooseText for documents indexed as loose text.
        documents.Add(DocumentMetadata.ForLooseText(
            documentId: 0,
            key: 42,
            documentStatistics: statistics));

        // Use ForObject instead when the document came from object tokenization.

        return new(documents);
    }

    protected override ValueTask<IndexNode> DeserializeIndexNodeHierarchyAsync(
        SerializedFieldIdMap serializedFieldIdMap,
        IIndexNodeFactory indexNodeFactory,
        CancellationToken cancellationToken)
    {
        var matches = new Dictionary<int, IReadOnlyList<IndexedToken>>
        {
            [0] =
            [
                new IndexedToken(
                    serializedFieldIdMap.Map(serializedFieldId: 1),
                    [new TokenLocation(tokenIndex: 0, start: 0, length: 4)])
            ]
        };

        var node = indexNodeFactory.CreateNode(
            "word".AsMemory(),
            ChildNodeMap.Empty,
            new DocumentTokenMatchMap(matches));

        return new(node);
    }
}
```

When reading a serialized node with children, create each child recursively and then pass a sorted `ChildNodeMapEntry[]` to `ChildNodeMap`:

``` csharp
var childNodes = storedChildren
    .OrderBy(child => child.Character)
    .Select(child => new ChildNodeMapEntry(child.Character, ReadNode(child)))
    .ToArray();

var childMap = childNodes.Length == 0
    ? ChildNodeMap.Empty
    : new ChildNodeMap(childNodes);
```

## Important details

Always deserialize into an empty index that was created with the current `FullTextIndexBuilder<TKey>` definition. This lets LIFTI map stored fields onto the fields configured in the new index.

Always use `SerializedFieldIdMap.Map` for field ids read while rebuilding node matches before recreating `IndexedToken` values. Field ids can change if fields were added or reordered between serialization and deserialization.

Preserve the internal document ids from the serialized data. Node matches refer to documents by those ids, and document metadata connects those ids back to the public keys.

Preserve token location ordering for each `IndexedToken`. Query execution expects token locations to describe the positions originally produced by tokenization.

Use `DocumentMetadata.ForLooseText` for entries indexed from loose text, and `DocumentMetadata.ForObject` for entries indexed from object tokenization so that object type and scoring metadata are restored.

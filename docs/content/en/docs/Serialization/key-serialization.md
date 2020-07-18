---
title: "Key Serialization"
linkTitle: "Key Serialization"
weight: 1
description: >
  The BinarySerializer can automatically handle keys of type `String`, `Int32`, `UInt32` and `Guid`. If you need a different type of key in your index, you will need to create a custom implementation of `IKeySerializer`.
---

Let's say you need to index more than one data point against an index:

* `UserId` of type `Int32` 
* `CompanyId` of type `Int16`

You would have a class or struct something like this:

``` csharp
public struct CompositeKey
{
    public CompositeKey(int userId, short companyId)
    {
        this.UserId = userId;
        this.CompanyId = companyId;
    }

    public int UserId { get; }
    public short CompanyId { get; }
}
```

Building and populating an index with this type as a key is as easy as ever:

``` csharp
// Create a full text index with a custom key type
var index = new FullTextIndexBuilder<CompositeKey>().Build();

// Index some sample data
await index.AddAsync(new CompositeKey(1, 9), "This is some text associated with A: fizz");
await index.AddAsync(new CompositeKey(2, 9), "Some buzz text for B");
await index.AddAsync(new CompositeKey(3, 11), "Text associated with C is both fizz and buzz");
```

> `CompositeKey` could also be the key of an object that you want to index. You'd just need to use the
> configure the `FullTextIndexBuilder` with the appropriate `WithKey` call when setting up the item tokenization options.

The only additional work comes when constructing the BinarySerializer - here you need to pass a custom `IKeySerializer` implementation 
so that the serializer knows how to read and write the data in your custom key:

``` csharp
// This would error with: No standard key serializer exists for type CompositeKey - 
//    please provide a custom implementation of IKeySerializer<> when serializing/deserializing.
// var serializer = new BinarySerializer<int>();

var serializer = new BinarySerializer<CompositeKey>(new CompositeKeySerializer());
using var stream = new MemoryStream();
```

Where your custom serializer is defined as:

``` csharp
public class CompositeKeySerializer : IKeySerializer<CompositeKey>
{
    public void Write(BinaryWriter writer, CompositeKey key)
    {
        writer.Write(key.UserId); // Int32
        writer.Write(key.CompanyId); // Int16
    }

    public CompositeKey Read(BinaryReader reader)
    {
        // The serialization framework will make sure this method is only
        // ever called when a key is ready to be read.
        // Ensure the data is read is read out in exactly the same order and with the same 
        // data types it was written.
        var userId = reader.ReadInt32();
        var companyId = reader.ReadInt16();

        return new CompositeKey(userId, companyId);
    }
}
```

And then serialization and deserialization will just work as normal:

``` csharp
// Serialize the index
await serializer.SerializeAsync(index, stream, disposeStream: false);

// Deserialize the index into a new instance
stream.Position = 0;
var newIndex = new FullTextIndexBuilder<CompositeKey>().Build();
await serializer.DeserializeAsync(newIndex, stream, disposeStream: false);

// Prove that the new index has the same contents and the keys have round-tripped
// Emits: only (3, 11) contains Fizz & Buzz
var match = newIndex.Search("fizz & buzz").Single();
Console.WriteLine($"Only ({match.Key.UserId}, {match.Key.CompanyId}) contains Fizz & Buzz");
```

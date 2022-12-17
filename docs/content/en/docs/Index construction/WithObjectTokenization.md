---
title: "WithObjectTokenization"
linkTitle: "WithObjectTokenization"
weight: 2
description: >
  Configure the index to accept a strongly typed object when indexing content.
---

## Example usage

``` csharp
public class Customer
{
    public int Id { get; set; }
    public string Name { get; set;}
    public string ProfileHtml { get; set;}
}

var index = new FullTextIndexBuilder<int>()
    .WithObjectTokenization<Customer>(o => o
        .WithKey(c => c.Id)
        .WithField("Name", c => c.Name)
        .WithField("Profile", c => c.ProfileHtml, textExtractor: new XmlTextExtractor())
    )
    .Build();

await index.AddAsync(new Customer { ... });
```

## WithKey

Each object configured against the index must have a key of the same type as the index's key. `WithKey` defines how this key is read from the object.

## WithField

An object needs one or more named fields configured from which to read text, each of which is configured using `WithField`.

### `name`

The unique name of the field in the index. This has two main uses subsequently:

1. In search results you are provided with a breakdown of where matches were found - the name of the field is included in this information.
1. When querying, you can restrict a part of the search to a specific field, e.g. `Profile=food` will only match the word food in the `Profile` field, and no others. See [Searching](../Searching/) for more information.

### `fieldTextReader`

A function capable of extracting the raw text from the object. Supported function definitions:

* `Func<T, string>`
* `Func<T, IEnumerable<string>>`

And their async equivalents:

* `Func<T, Task<string>>`
* `Func<T, Task<IEnumerable<string>>>`

### `tokenizationOptions`

Equivalent to [WithDefaultTokenization](./WithDefaultTokenization) but for use exclusively with this field. Left null, the default tokenizer for the index will be used.

### `textExtractor`

Equivalent to [WithTextExtraction](./WithTextExtraction) but for use exclusively use with this field. Left null, the default text extractor for the index will be used.

### `thesaurusOptions`

Equivalent to [WithDefaultThesaurus](./WithDefaultThesaurus) but for use exclusively with this field. Left null, the default thesaurus builder for the index will be used.

## Indexing multiple object types

It is possible to index multiple types against an index, however you need to consider a couple of constraints.

### 1. Extracted keys need to be unique across all object types.

If you are indexing disparate object types with overlapping ids, you could consider using a composite key type in the index, e.g.

``` csharp
public enum EntryKind
{
    Customer,
    Product
}

var index = new FullTextIndexBuilder<( EntryKind kind, int id )>()
    .WithObjectTokenization<Customer>(o => o
        .WithKey(c => (EntryKind.Customer, c.Id))
        .WithField("CustomerName", c => c.Name)
    )
    .WithObjectTokenization<Product>(o => o
        .WithKey(p => (EntryKind.Product, p.Id))
        .WithField("ProductName", p => p.Name)
    )
    .Build();
```

> To serialize an index with a composite key, you will need to use a custom `IKeySerializer` - see [Key Serialization](../Serialization/key-serialization).

### 2. Field names must be unique across all object types

You will get an error if you try to register the same name twice, even in separate `WithObjectTokenization` calls.

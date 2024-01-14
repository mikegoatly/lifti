---
title: "Object tokenization"
linkTitle: "Object tokenization"
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
        .WithField("Name", c => c.Name, scoreBoost: 1.5D)
        .WithField("Profile", c => c.ProfileHtml, textExtractor: new XmlTextExtractor())
        .WithDynamicFields("Tags", c => c.TagDictionary, "Tag_")
        .WithDynamicFields(
            "Questions", 
            c => c.Questions, 
            q => q.QuestionName, 
            q => q.QuestionResponse, 
            "Question_",
            scoreBoost: 1.8D)
        .WithScoreBoosting(
            boost => boost
                .Freshness(c => c.UpdatedDate, 2D)
                .Magnitude(c => c.Rating, 2D))
    )
    .Build();

await index.AddAsync(new Customer { ... });
```

## `WithKey`

Each object configured against the index must have a key of the same type as the index's key. `WithKey` defines how this key is read from the object.

## `WithField`

An object can be configured with one *static* fields that are known at compile time. The `WithField` method overloads allow for static fields to be defined.

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

### `scoreBoost`

The multiplier to apply to the score of this field when ranking results. The default value of 1 is equivalent to no boosting.

## `WithDynamicFields`

In addition to the static fields configured using `WithField`, it is possible to configure dynamic fields that are not known at compile time. The `WithDynamicFields`
overloads allow for dynamic field readers to be defined, each of which will be invoked to retrieve the field names for the object being indexed.

> **Important:** LIFTI only supports a maximum of 255 unique field names per index, whether they are dynamic or defined statically using `WithField`.
Because dynamic fields are not known at compile time, it is possible to exceed this limit if you are not careful. If you do exceed this limit, an exception will be thrown 
when you try to index an object.

### `dynamicFieldReaderName`

The unique name of the dynamic field reader. Dynamic fields are registered in the index against this name so that when the index is deserialized, the dynamic fields
can be rehydrated against the correct configuration.

### `dynamicFieldReader`

A function capable of extracting the dynamic field information from the object type `T`.

You can provide a function that returns a dictionary of name/value pairs, where the key becomes the field name
and the value the text being indexed against it:

* `Func<T, IDictionary<string, string>?>`
* `Func<T, IDictionary<string, IEnumerable<string>>>`

Or you can provice a function that returns a collection of *child objects*:

* `Func<T, ICollection<TChild>?>`
* `Func<T, ICollection<TChild>?>`

These last two overloads also require you provide two more delegates via the `getFieldName` and `getFieldText` parameters.
These delegates are used to extract the field name and text from each child object.

### `fieldNamePrefix`

The prefix to use when constructing the field name. This is useful when the dynamic fields can produce the same field name as a static field,
or a dynamic field from another dynamic field reader.

### Other `WithDynamicFields` parameters

The `tokenizationOptions`, `textExtractor`, `thesaurusOptions` and `scoreBoost` parameters are equivalent to their `WithField` counterparts.

## `WithScoreBoosting`

Configures the score boosting options for the object type. These allow you to promote documents associated to objects based on related data.

### `Freshness`

Freshness boosting allows you to boost results based on a date associated to the object. For example, assuming all the documents have exactly the same text
and a multiplier of 3 is specified, then the score of the newest document will be 3 times higher than the oldest.

### `Magnitude`

Magnitude boosting allows you to boost results based on a numeric value associated to the object. For example, if you used this with a "star rating" property,
documents with a higher rating will be more likely to appear nearer the top of search results.

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

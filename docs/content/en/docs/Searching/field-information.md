---
title: "Field Information"
linkTitle: "Field Information"
weight: 5
description: >
  You can query the index to get information about the fields that have been indexed.
---

A `FullTextIndex` class exposes a `FieldLookup` property of type `IIndexedFieldLookup` that can be used to query the index for 
information about the fields that have been indexed.

`IIndexedFieldLookup` exposes the following methods:

## `DefaultField`

The id of the default field used when an `IFullTextIndex{T}.AddAsync(T, string, System.Threading.CancellationToken)` overload has been used, as opposed to indexing text read from properties of object.

## `GetFieldForId(byte id)`

Gets the configured name for a field id.

## `GetFieldInfo(string fieldName)`

Gets the configuration required for indexing a named field, including the `Tokenization.TextExtraction.ITextExtractor` and `Tokenization.IIndexTokenizer` instances to use when processing the field's text.

### `IndexedFieldDetails`

This abstract class contains information about a field that has been configured for indexing.

#### Properties

- **Id**: Gets the id of the field.
- **Name**: Gets the name of the field.
- **ObjectType**: Gets the type of the object the field is registered for.
- **FieldKind**: Gets the kind of field this instance represents, either `FieldKind.Static` or `FieldKind.Dynamic`.
- **TextExtractor**: Gets the `ITextExtractor` used to extract sections of text from this field.
- **Tokenizer**: Gets the `IIndexTokenizer` that should be used when tokenizing text for the field.
- **Thesaurus**: Gets the `IThesaurus` that should be used to expand tokens when processing text for this field.
- **DynamicFieldReaderName**: Gets the name of the dynamic field reader that generated this field. If this field is not a dynamic field, this will be `null`.

#### Methods

- **ReadAsync(object item, CancellationToken cancellationToken)**: Reads the text for the field from the specified item. The item must be of the type specified by the `ObjectType` property.


## `IsKnownField(Type objectType, string fieldName)`

Returns `true` if the given field name is known to the index and associated to the given object type, whether statically defined at index creation, or dynamically registered during indexing.

## `AllFieldNames`

Gets the names of all fields configured in the index, including any dynamic fields that have been registered during the indexing of objects.

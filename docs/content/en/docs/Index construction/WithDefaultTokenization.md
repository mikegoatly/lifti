---
title: "WithDefaultTokenization"
linkTitle: "WithDefaultTokenization"
weight: 2
description: >
  Specifies the default tokenization options that should be used when searching or indexing when no other options are provided.
---

## Example usage

``` csharp
var index = new FullTextIndexBuilder<int>()
    .WithDefaultTokenization(o =>o
        .AccentInsensitive(true) // Default
        .CaseInsensitive(true) // Default
        .SplitOnPunctuation(true) // Default
        .SplitOnCharacters('%', '#', '@')
        .WithStemming()
    )
    .Build();
```

## TokenizerBuilder methods

### Word break modifiers

A tokenizer will always break words on separator (`Char.IsSeparator`) or control (`Char.IsControl`) characters.

#### `SplitOnPunctuation(bool)`

`true`: **Default** The tokenizer will split words on punctuation characters (e.g. those that match `Char.IsPunctuation(char)`)

`false`: Only characters explicitly specified using `SplitOnCharacters` will be treated as word breaks.

#### `SplitOnCharacters(params char[])`

Allows for additional characters to cause word breaks for a tokenizer. E.g. `SplitOnCharacters('$', '£')`.

### Text Normalization

#### `AccentInsensitive(bool)`

`true`: **Default** The tokenizer will normalize characters with diacritics to common form. e.g. `aigües` and `aigues` will be equivalent. 
Additionally, characters that can be logically expressed as two characters are expanded, e.g. `laering` will be equivalent to `læring`.

`false`: The tokenizer will be accent sensitive. Searching for `aigües` will not match `aigues`.

#### `CaseInsensitive(bool)`

`true`: **Default** The tokenizer will normalize all characters to uppercase. e.g. `Cat` and `cat` will be equivalent.

`false`: The tokenizer will be case sensitive. Searching for `Cat` will match `Cat` but not `cat`.

#### `WithStemming(bool)`

`true`: Words will be stemmed using an implementation of the Porter Stemmer algorithm. For example, `ABANDON`, `ABANDONED` and `ABANDONING` will all
be treated as `ABANDON`. Currently only English is supported.

`false`: **Default** No stemming will be performed on words.

---
title: "Default tokenization"
linkTitle: "Default tokenization"
weight: 2
description: >
  Specifies the default tokenization options that should be used when searching or indexing when tokenization options are not explicitly specified for an object type.
---

## Example usage

``` csharp
var index = new FullTextIndexBuilder<int>()
    .WithDefaultTokenization(o =>o
        .AccentInsensitive(true) // Default
        .CaseInsensitive(true) // Default
        .SplitOnPunctuation(true) // Default
        .SplitOnCharacters('%', '#', '@')
        .IgnoreCharacters('<', '>')
        .WithStemming()
    )
    .Build();
```

## TokenizerBuilder methods

### Text Normalization

#### `IgnoreCharacters(char[])`

Configures the tokenizer to ignore certain characters as it is parsing input.
Ignoring characters will prevent them from acting as split characters, so care needs to be taken that your source
text doesn't words delimited only by ignored characters, otherwise you may end up unexpectedly joining search terms
into one. For example, ignoring the `'` character will mean that `O'Reilly` will be tokenized 
as `OReilly`, but if your source text also contains `she said'hello'` then `she` and 
`saidhello` will treated as tokens.

#### `AccentInsensitive(bool)`

`true`: **Default** The tokenizer will normalize characters with diacritics to common form. e.g. `aigües` and `aigues` will be equivalent. 
Additionally, characters that can be logically expressed as two characters are expanded, e.g. `laering` will be equivalent to `læring`.

`false`: The tokenizer will be accent sensitive. Searching for `aigües` will not match `aigues`.

#### `CaseInsensitive(bool)`

`true`: **Default** The tokenizer will normalize all characters to uppercase. e.g. `Cat` and `cat` will be equivalent.

`false`: The tokenizer will be case sensitive. Searching for `Cat` will match `Cat` but not `cat`.

#### `WithStemming()`

Words will be stemmed using an implementation of the Porter Stemmer algorithm. For example, `ABANDON`, `ABANDONED` and `ABANDONING` will all
be treated as `ABANDON`. Currently only English is supported.

A [custom stemmer](../../custom-stemmers/) can be used by implementing an `IStemmer` and using `WithStemming(new YourStemmerImplementation())`.

### Word break modifiers

A tokenizer will always break words on separator (`Char.IsSeparator`) or control (`Char.IsControl`) characters.

#### `SplitOnPunctuation(bool)`

`true`: **Default** The tokenizer will split words on punctuation characters (e.g. those that match `Char.IsPunctuation(char)`)

`false`: Only characters explicitly specified using `SplitOnCharacters` will be treated as word breaks.

#### `SplitOnCharacters(params char[])`

Allows for additional characters to cause word breaks for a tokenizer. E.g. `SplitOnCharacters('$', '£')`.

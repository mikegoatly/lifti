---
title: "WithDefaultThesaurus"
linkTitle: "WithDefaultThesaurus"
weight: 5
description: >
  Prescribes how the index should treat terms as synonymous when they are being added to the index.
---

## Defining a thesaurus

A thesaurus is a tool used to organize and categorize words based on their meanings and relationships. It can be built using
the `ThesaurusBuilder` class, which allows you to add both *synonyms* and *hypernyms*.

### `AddSynonyms(ICollection<string> synonyms)`

The AddSynonyms method is used to instruct the thesaurus to expand any of the given synonyms when one of them indexed. For example,
if you add the synonyms `dog`, `doggy`, `puppy`, and `pup` to the thesaurus and include the terms `"I love my doggy"` and `"We got a new puppy today"`
in the index, a search for `"dog"` would return both of these entries because `doggy` and `puppy` are synonymous with `dog`.

### `AddHyponyms(string word, ICollection<string> hyponyms)`

The `AddHyponyms` method is used to instruct the thesaurus to expand the search to include any words that are more specific or narrower
in meaning compared to the given term. These narrower terms are known as *hyponyms*.

For example, if you add the hyponyms `poodle` and `beagle` to the thesaurus for the term `dog` using:

``` csharp
AddHyponyms("dog", "poodle", "beagle");
```

* searching for `"dog"` would return entries that contain `poodle` and `beagle` because they are hyponyms of `dog`.
* searching for `"poodle"` or `"beagle"` will return only entries that explicitly contain those terms - other mentions of dog,
  e.g. "Dogs are a type of domestic animal" will not be returned

### `AddHypernyms(string word, ICollection<string> hypernyms)`

The `AddHypernyms` method is used to instruct the thesaurus to expand the search to include any words that are more general or broader
in meaning compared to the given terms. These broader terms are known as *hypernyms*.

For example, if you add the hypernyms `mammal` and `canine` to the thesaurus for the term `dog`:

* searching for `"mammal"` would return entries that contain `dog` because `mammal` is a hypernym of `dog`.
* searching for `"dog"` will return only entries that explicitly contain `dog` - other mentions of `mammal`, e.g. "Mammals are warm blooded" will not be returned

## Default thesaurus

`FullTextIndexBuilder<TKey> WithDefaultThesaurus(Func<ThesaurusBuilder, ThesaurusBuilder> thesaurusBuilder)`

Builds the default thesaurus to use for the index. This thesaurus will be used for text added directly to the index and also fields
that have no explicit thesaurus defined for them.

``` csharp
var index = new FullTextIndexBuilder<int>()
    .WithDefaultThesaurus(o => o.AddHyponyms("dog", "poodle", "beagle"))
    .Build();
```

## Field-level thesaurus

A thesaurus can be configured specifically for an object's field - in this example the hyponyms will only be applied to the `Content` field:

``` csharp
var index = new FullTextIndexBuilder<int>()
    .WithObjectTokenization<Book>(
    o => o
        .WithKey(i => i.Id)
        .WithField("Content", i => i.Content, thesaurusOptions: o => o.AddHyponyms("dog", "poodle", "beagle")
        .WithField("Title", i => i.Title));
```

## Interplay between the thesaurus and tokenizer

The values in a thesaurus are automatically passed through the tokenizer for the relevant field. For example, an index constructed as:

``` csharp
var index = new FullTextIndexBuilder<int>()
    .WithDefaultTokenizer(o => o.CaseInsensitive())
    .WithDefaultThesaurus(o => o.AddSynonyms("conduct", "conduction"))
    .WithObjectTokenization<Book>(
    o => o
        .WithKey(i => i.Id)
        .WithField("Content", i => i.Content, tokenizationOptions: o => o.WithStemming())
        .WithField("Title", i => i.Title, tokenizationOptions: o => o.CaseInsensitive(false)));
```

Would end up with 3 variations of the thesaurus values:

* Once for the default tokenizer with case insensitivity applied
* Once for the "Content" with stemming applied to the words
* Once for the "Title" field without case insensitivity applied
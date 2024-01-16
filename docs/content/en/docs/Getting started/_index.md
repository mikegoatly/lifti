---
title: "Getting Started"
linkTitle: "Getting Started"
weight: 1
description: >
  The simplest possible way to create a LIFTI index, index some text in it and retrieve search results.
---

Perhaps the simplest way to work with LIFTI is to index text against a key that means something in some other system.

In this example, we're just indexing three pieces of text against 3 integer keys:

``` c#
// Create a full text index with default settings and integer keys
var index = new FullTextIndexBuilder<int>().Build();

// Index keys with their associated text
await index.AddAsync(1, "This is some text associated with A: fizz");
await index.AddAsync(2, "Some buzz text for B");
await index.AddAsync(3, "Text associated with C is both fizz and buzz");

```

You can search in this index using:

``` c#
// Search for documents containing both Fizz *and* Buzz
var results = index.Search("Fizz Buzz").ToList();

// Output: Documents with both Fizz and Buzz: 1
Console.WriteLine($"Documents with both Fizz and Buzz: {results.Count}");

// Search for documents containing both Fizz *or* Buzz
results = index.Search("Fizz | Buzz").ToList();

// Outputs: Documents with Fizz or Buzz: 3
Console.WriteLine($"Documents with Fizz or Buzz: {results.Count}");
```

Each set of results returns the keys that the text was indexed against. For example, the first set of results will return a key of 3, 
as that is the only key that was indexed with both "fizz" and "buzz".

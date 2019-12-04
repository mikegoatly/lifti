---
title: Indexing text against keys
permalink: /indexing/indexing-text/
---

_TODO: Introduce concept of keys in an index_

Perhaps the simplest way to work with LIFTI is to index text against a key that means something in some other system.

In this example, we're just indexing three pieces of text against 3 integer keys:

``` c#
// Create a full text index with default settings and integer item keys
var index = new FullTextIndexBuilder<int>().Build();

// Index item keys with their associated text
await index.AddAsync(1, "This is some text associated with A: fizz");
await index.AddAsync(2, "Some buzz text for B");
await index.AddAsync(3, "Text associated with C is both fizz and buzz");

```

You can search in this index using:

``` c#
// Search for text containing both Fizz *and* Buzz
var results = index.Search("Fizz Buzz").ToList();

// Output: Items with both Fizz and Buzz: 1
Console.WriteLine($"Items with both Fizz and Buzz: {results.Count}");

// Search for text containing both Fizz *or* Buzz
results = index.Search("Fizz | Buzz").ToList();

// Outputs: Items with Fizz or Buzz: 3
Console.WriteLine($"Items with Fizz or Buzz: {results.Count}");
```

Each set of results returns the keys that the text was indexed against. For example, the first set of results will return a key of 3, 
as that is the only key that was indexed with both "fizz" and "buzz".

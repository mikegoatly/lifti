[![Build Status](https://goatly.visualstudio.com/LIFTI/_apis/build/status/mikegoatly.lifti?branchName=master)](https://goatly.visualstudio.com/LIFTI/_build/latest?definitionId=14&branchName=master)

# LIFTI

A lightweight full text indexer for .NET

This is very much a work in progress, and a re-imagining of the [original LIFTI project](https://github.com/mikegoatly/lifti-codeplex) from many years ago.

## Simplest possible quick start

``` c#
// Create a full text index with default settings
var index = new FullTextIndexBuilder<string>().Build();

// Index
index.Add("A", "This is some text associated with A: fizz");
index.Add("B", "Some buzz text for B");
index.Add("C", "Text associated with C is both fizz and buzz");

// Search for text containing both Fizz *and* Buzz
var results = index.Search("Fizz Buzz").ToList();

// Output: Items with both Fizz and Buzz: 1
Console.WriteLine($"Items with both Fizz and Buzz: {results.Count}");

// Search for text containing both Fizz *or* Buzz
results = index.Search("Fizz | Buzz").ToList();

// Outputs: Items with Fizz or Buzz: 3
Console.WriteLine($"Items with Fizz or Buzz: {results.Count}");
```

## CI nuget feed

If you want to try out the early versions of this, you can download the Lifti.Core package from the CI package feed: https://goatly.pkgs.visualstudio.com/LIFTI/_packaging/lifti-ci/nuget/v3/index.json

## Goals
1) Provide a very simple way to build an in-memory full text index.
2) netstandard2 compatibility
3) Persistance via explicit serialization/deserialization points. Automatic persistance to a backing store upon changes to the index is out of scope.

### Non-goals
1) Distributed transactions are out of scope and don't make much sense anyway given automatic persistance is not in scope. (the old version did support this but I don't think anyone actually used it though - I know didn't :))

## Significant differences between new and old implementations
The old implementation followed a very simple tree structure where every node in the tree linked to its descendants via relevant chars, e.g.:

```
A
╚P
 ╠E (matches APE)
 ╚P
  ╚L
   ╚E (matches APPLE)
```

The new implementation expands upon this by allowing a node to also contain a series of "intra-node" characters - this reduces the number of
nodes in the tree, reducing the overall memory footprint and allows for potential gains in search times. (less object traversal required):

```
AP
 ╠E (matches APE)
 ╚PLE (matches APPLE)
```

## Performance testing vs old implementation

|                                    Method |     Mean |    Error |   StdDev | Rank |      Gen 0 |      Gen 1 |     Gen 2 | Allocated |
|------------------------------------------ |---------:|---------:|---------:|-----:|-----------:|-----------:|----------:|----------:|
| NewCodeIndexingAlwaysSupportIntraNodeText | 464.9 ms | 1.310 ms | 1.226 ms |    3 | 25000.0000 |  7000.0000 | 1000.0000 | 144.85 MB |
|      NewCodeIndexingAlwaysIndexCharByChar | 475.6 ms | 2.606 ms | 2.310 ms |    4 | 27000.0000 |  8000.0000 | 1000.0000 |  155.7 MB |
| NewCodeIndexingIntraNodeTextAt4Characters | 446.4 ms | 3.884 ms | 3.633 ms |    1 | 26000.0000 |  7000.0000 | 2000.0000 | 145.32 MB |
| NewCodeIndexingIntraNodeTextAt2Characters | 456.7 ms | 3.035 ms | 2.839 ms |    2 | 25000.0000 |  7000.0000 | 1000.0000 | 144.86 MB |
|                        legacycodeindexing | 481.6 ms | 5.867 ms | 5.488 ms |    4 | 39000.0000 | 10000.0000 | 1000.0000 | 228.35 MB |

And with word stemming (a Porter stemming implementation) enabled, the difference is even more noticable:

|                                    Method |     Mean |    Error |   StdDev | Rank |      Gen 0 |      Gen 1 |     Gen 2 | Allocated |
|------------------------------------------ |---------:|---------:|---------:|-----:|-----------:|-----------:|----------:|----------:|
| NewCodeIndexingIntraNodeTextAt4Characters | **523.9 ms** | 6.450 ms | 9.655 ms |    1 | 27000.0000 |  7000.0000 | 2000.0000 | 148.46 MB |
|                        LegacyCodeIndexing | 730.6 ms | 3.852 ms | 5.646 ms |    2 | 58000.0000 | 14000.0000 | 2000.0000 | 336.37 MB |

Notes: 
- Each execution populates an index with 200 Wikipedia articles. In other words, in the best case above, it takes 446ms to completely index 200 Wikipedia pages (~7Mb of content) into memory.
- The new implementation additionally normalizes characters to latin representations (i.e. allowing for case insensitive searching) and there is no equivalent of this in the previous implementation.

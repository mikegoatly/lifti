[![Build Status](https://goatly.visualstudio.com/LIFTI/_apis/build/status/mikegoatly.lifti?branchName=master)](https://goatly.visualstudio.com/LIFTI/_build/latest?definitionId=14&branchName=master)

# LIFTI

A lightweight full text indexer for .NET

This is very much a work in progress, and a re-imagining of the [original LIFTI project](https://github.com/mikegoatly/lifti-codeplex) from many years ago.

Read the documentation [here](https://mikegoatly.github.io/lifti)

## Try it out!

[Use LIFTI in a Blazor app](https://liftidemo.z22.web.core.windows.net/wikipedia-search) - try out various queries against Wikipedia content

## Simplest possible quick start

``` powerhshell
Install-Package Lifti.Core -Version 2.0.0-beta9 -AllowPrereleaseVersions
```

``` c#
// Create a full text index with default settings
var index = new FullTextIndexBuilder<string>().Build();

// Index
await index.AddAsync("A", "This is some text associated with A: fizz");
await index.AddAsync("B", "Some buzz text for B");
await index.AddAsync("C", "Text associated with C is both fizz and buzz");

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

# lifti
A lightweight full text indexer for .NET

This is very much a work in progress, and a re-imagining of the [original LIFTI project](https://github.com/mikegoatly/lifti-codeplex) from many years ago.

More information will follow as the code is fleshed out.

Note:
There is currently a dependency on preview BCL nuget packages (specifically Microsoft.Bcl.HashCode).

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

Notes: 
- Each execution populates an index with 200 Wikipedia articles. In other words, in the best case above, it takes 446ms to completely index 200 Wikipedia pages (~7Mb of content) into memory.
- The new implementation additionally normalizes characters to latin representations (i.e. allowing for case insensitive searching) and there is no equivalent of this in the previous implementation.
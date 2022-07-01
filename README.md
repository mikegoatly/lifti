[![Build Status](https://goatly.visualstudio.com/LIFTI/_apis/build/status/mikegoatly.lifti?branchName=master)](https://goatly.visualstudio.com/LIFTI/_build/latest?definitionId=14&branchName=master)

# LIFTI

A lightweight full text indexer for .NET

## Documentation

[Read the documentation](https://mikegoatly.github.io/lifti/docs) - there's lots of useful information and examples there.

[Check out some sample code](https://github.com/mikegoatly/lifti/tree/master/samples/TestConsole) - the repo contains examples that can be run as a console application.

[![buymeacoffee](https://user-images.githubusercontent.com/4577868/176899270-00846262-8323-4f13-9c6a-d655a30cbca3.png)](https://www.buymeacoffee.com/mikegoatly)


## Try it out!

[Use LIFTI in a Blazor app](https://mikegoatly.github.io/lifti/blazor-sample) - try out various queries against Wikipedia content

## Simplest possible quick starts

``` powershell
Install-Package Lifti.Core
```

[Indexing loose text](https://mikegoatly.github.io/lifti/docs/getting-started/)

[Indexing objects](https://mikegoatly.github.io/lifti/docs/getting-started/indexing-objects/)

## Contribute

It would be great to have more people contributing to LIFTI - how can you help?

* Create issues for bugs you find - **level 1**
* Create feature suggestions - **level 2**
* Create pull requests for documentation changes - **level 3**
* Create pull requests for bug fixes or features - **boss level**

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

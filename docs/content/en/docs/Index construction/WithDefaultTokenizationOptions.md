---
title: "WithDefaultTokenizationOptions"
linkTitle: "WithDefaultTokenizationOptions"
weight: 2
description: >
  Specifies the default tokenization options that should be used when searching or indexing when no other options are provided.
---

`FullTextIndexBuilder<TKey> WithDefaultTokenizationOptions(Func<TokenizationOptionsBuilder, TokenizationOptionsBuilder> optionsBuilder)`

## Example usage

``` csharp
var index = new FullTextIndexBuilder<int>()
    .WithDefaultTokenizationOptions(o =>o
        .AccentInsensitive()
        .CaseInsensitive()
        .SplitOnPunctuation()
        .SplitOnCharacters('%', '#', '@')
        .WithStemming()
        .XmlContent()
    )
    .Build();
```

### 
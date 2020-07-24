---
title: "WithTextExtraction"
linkTitle: "WithTextExtraction"
weight: 6
description: >
  Text extraction is the process by which fragments of text are lifted from a larger body of text prior to tokenization. 
---

For example, the text being indexed may be an XML or HTML document and you may only want to index the text content of the elements:

``` csharp
var index = new FullTextIndexBuilder<int>()
    .WithTextExtraction<XmlTextExtractor>()
    .Build();
```

Text extraction is only applied when _indexing_ text, i.e. calls to the `IFullTextIndex.AddAsync` overloads. When searching, text extraction is never applied to any query text.

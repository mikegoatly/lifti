---
title: "Search Result Scoring"
linkTitle: "Result Scoring"
weight: 6
description: >
  How does LIFTI score results?
---

LIFTI uses a version of the [Okapi BM25](https://en.wikipedia.org/wiki/Okapi_BM25) algorithm to score search results. At the simplest level this means that search results will come back ordered by relevance. Fuzzy matching affects the 
scores for search results depending on the distance between the target word and the search term. 

Once nice feature of LIFTI is that in you also get each field scored independently. The overall score for a document is just a sum of these, but you could easily just re-order the results by one field over another should you so wish. 

`OrderByField` is a convenience method that can re-order results by a single field:

``` csharp
var index = new FullTextIndexBuilder<int>()
    .WithObjectTokenization<Customer>(o => o
        .WithKey(c => c.Id)
        .WithField("Name", c => c.Name)
        .WithField("Profile", c => c.ProfileHtml, textExtractor: new XmlTextExtractor())
    )
    .Build();

await index.AddAsync(new Customer { Id = 1, Name = "Joe Bloggs", ProfileHtml = "<a>Something else something</a>" });
await index.AddAsync(new Customer { Id = 2, Name = "Joe Something", ProfileHtml = "<a>Something else</a>" });

// Searching for "Something" will result in ID 2 being ordered before ID 1.
// "Something" appears twice in each document overall, however document 2 has fewer words, therefore the matches
// are more statistically significant.
var results = index.Search("something");
PrintSearchResults(results); 

// Output
// 2
// 1

// But if you only consider the "Profile" field, then the Something only appears once in document 2,
// therefore document 1 will come first.
results = results.OrderByField("Profile");
PrintSearchResults(results);

// Output
// 1
// 2
```
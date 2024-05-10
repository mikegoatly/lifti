---
title: "Recipes"
linkTitle: "Recipes"
weight: 1
description: >
  This cookbook provides a collection of examples to help you get started with LIFTI. Whether you're building a search engine, a knowledge base, or any application that deals with large volumes of textual data, these recipes will guide you through common tasks and scenarios.
---

Each recipe in this cookbook provides a self-contained code example, along with detailed explanations, background information, and step-by-step instructions. You'll find solutions for common challenges, best practices, and time-saving techniques contributed.

To contribute, see [Documentation Contributions](https://github.com/mikegoatly/lifti?tab=readme-ov-file#contribute) for more details.

## Indexing a collection of strings

> [.NET Fiddle](https://dotnetfiddle.net/ih1O1X)

Scenario: You just have a list of strings that you need to search across.

```csharp
string[] strings = [
  "contains fizz",
  "contains buzz",
  "contains fizz and buzz."
];

// Create a full text index with default settings and integer keys.
// The key will be the index into the string collection
var index = new FullTextIndexBuilder<int>().Build();

// Index all the strings in the collection
index.BeginBatchChange();
for (var i = 0; i < strings.Length; i++) 
{
  await index.AddAsync(i, strings[i]);
}

await index.CommitBatchChangeAsync();

// Search the index for strings containing both fizz AND buzz
var results = index.Search("fizz & buzz");

// Prints "1"
Console.WriteLine($"{results.Count}");

// Search the index for strings containing fizz OR buzz
results = index.Search("fizz | buzz");

// Output the indexes of the matching strings
// Prints "3"
Console.WriteLine($"{results.Count}");
```

## Indexing text from an object

> [.NET Fiddle](https://dotnetfiddle.net/d6bq05)

Scenario: You have an object with string properties that you need to index:

```csharp
var bookIndex = new FullTextIndexBuilder<int>()
  // Configure the index to work with Book objects
  .WithObjectTokenization<Book>(
      options => options
        // Specifies the BookId property as the key for each document in the index.
        .WithKey(b => b.BookId)
        // Define a field named "Title"
        .WithField("Title", b => b.Title)
        // Define a field for "Authors" (arrays of strings are treated as a single body of text)
        .WithField("Authors", b => b.Authors)
        // Define a field for "Synopsis"
        .WithField("Synopsis", b => b.Synopsis))
  .Build();
    
// Add an array of your books to the index (this could be retrieved from a database, file, etc.)
var books = new[]
{
  new Book
  {
    BookId = 1,
    Title = "The Three Body Problem",
    Authors = new[] { "Liu Cixin" },
    Synopsis = "The Three-Body Problem (Chinese: 三体; literally: 'Three-Body'; pinyin: sān tǐ) is a hard science fiction novel..."
  },
  new Book
  {
    BookId = 2,
    Title = "Dragons of Autumn Twilight",
    Authors = new[] { "Margaret Weis", "Tracy Hickman" },
    Synopsis = "Dragons of Autumn Twilight is a 1984 fantasy novel by American writers Margaret Weis and Tracy Hickman..."
  },
  // ... other books
};
  
await bookIndex.AddRangeAsync(books);
  
// Search for "novel" across all indexed fields (Title, Authors, Synopsis)
var results = bookIndex.Search("novel");

// Prints:
// Matched documents: 2, 1
// with respective scores: 0.1844777907113601, 0.18021514599659186
Console.WriteLine($"Matched documents: {String.Join(", ", results.Select(i => i.Key))}");
Console.WriteLine($"with respective scores: {string.Join(", ", results.Select(i => i.Score))}");

// Search for "the" specifically in the Title field
results = bookIndex.Search("title=the");

// Prints: "Matched documents: 1"
Console.WriteLine("Matched documents: " + string.Join(", ", results.Select(i => i.Key)));
```

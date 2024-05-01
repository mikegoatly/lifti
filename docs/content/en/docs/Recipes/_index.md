---
title: "Recipes"
linkTitle: "Recipes"
weight: 1
description: >
  This cookbook provides a collection of examples to help you get started with LIFTI. Whether you're building a search engine, a knowledge base, or any application that deals with large volumes of textual data, these recipes will guide you through common tasks and scenarios.
---

Each recipe in this cookbook provides a self-contained code example, along with detailed explanations, background information, and step-by-step instructions. You'll find solutions for common challenges, best practices, and time-saving techniques contributed.

To contribute, see [Documentation Contributions](https://github.com/mikegoatly/lifti?tab=readme-ov-file#contribute) for more details.

## Creating a Simple LIFTI Index

This recipe demonstrates the basic steps for creating a LIFTI full-text index, adding text documents to the index, and performing searches.

### Create a new index 

``` c#
// Create a full text index with default settings and integer keys
var index = new FullTextIndexBuilder<int>().Build();
```
This line creates a new LIFTI full-text index using the [FullTextIndexBuilder](../index-construction) class with integer keys. 

### Add text to index 

``` c#
// Index keys with their associated text
await index.AddAsync(1, "Some text associated with 1 and contains fizz");
await index.AddAsync(2, "Some text associated with 2 and contains buzz");
await index.AddAsync(3, "Some text associated with 3 and contains both fizz and buzz");
```
These lines add three text documents to the index using the [AddAsync](../indexing-mutations) method. Each document is identified by a unique integer key (1, 2, and 3) and associated with a text string.

### Search in index 
``` c#
// Search for documents containing both fizz *and* buzz. 
var results = index.Search("fizz buzz").ToList();

// Output the number of documents with both fizz and buzz: 1
Console.WriteLine($"Documents with both fizz and buzz: {results.Count}");

// Search for documents containing fizz *or* buzz
results = index.Search("fizz | buzz").ToList();

// Output the number of documents with fizz or buzz: 3
Console.WriteLine($"Documents with fizz or buzz: {results.Count}");
```
The search results include the original keys ( 1, 2, or 3) associated with the text documents. For example, the first search will only find documents with key 3 because that's the only document containing both fizz and buzz. The second search finds all three documents because they all contain either fizz or buzz or both.

## Indexing Text from Object Properties

This recipe demonstrates how to use LIFTI to extract and index text from properties within your objects. This will enable you to search for content based on the data stored in your objects, instead of using separate IDs. Each property you configure for indexing is called a "field."

### Given a class Called Book
 ``` c#
public class Book
{
  public int BookId { get; set; }
  public string Title { get; set; }
  public string[] Authors { get; set; }
  public string Synopsis { get; set; } 
}
 ```
This is a sample class called Book that contains properties you might want to search within.

 ### Build the Index
 ``` c#
// Books are indexed by their BookId property (integer key)
var bookIndex = new FullTextIndexBuilder<int>()
//Configures the index to work with Book objects
  .WithObjectTokenization<Book>(
    options => options
      // Specifies the BookId property as the key for each document in the index.
      .WithKey(b => b.BookId)
      // defines a field named "Title" that extracts text from the Title property of each book. The WithStemming option applies stemming during tokenization, which can improve search accuracy.
      .WithField("Title", b => b.Title,
          tokenOptions => tokenOptions.WithStemming())
      // Defines a field for "Authors" (handles arrays of strings)
      .WithField("Authors", b => b.Authors)
      // Defines a field for "Synopsis" with stemming
      .WithField("Synopsis", b => b.Synopsis,
          tokenOptions => tokenOptions.WithStemming()))
  .Build();
 ```
This code creates a LIFTI full-text index configured to extract text from specific properties of Book objects.

 ### Index the Books
 ``` c#
//An array of Book objects is created
var books = new[]
{
  new Book
  {
    BookId = 1,
    Title = "The Three Body Problem",
    Authors = new[] { "Liu Cixin" },
    Content = "The Three-Body Problem (Chinese: 三体; literally: 'Three-Body'; pinyin: sān tǐ) is a hard science fiction novel..."
  },
  // ... other books
};
//Adds all the books in the array to the index
await bookIndex.AddRangeAsync(books);
 ```
 This code adds Book objects to the index using the `AddRangeAsync` method.

 ### Search the Index
 ``` c#
// Search for "first" across all indexed fields (Title, Authors, Content)
var results = bookIndex.Search("first");
Console.WriteLine(
  "Matched documents: " +
  string.Join(", ", results.Select(i => i.Key)) +
  " with respective scores: " +
  string.Join(", ", results.Select(i => i.Score)));

// Search for "the" specifically in the Title field
results = bookIndex.Search("title=the");
Console.WriteLine("Matched documents: " + string.Join(", ", results.Select(i => i.Key)));

 ```
This code searches the index using keywords or phrases. The results include the document keys (book IDs) and relevancy scores for each match.
---
title: "Extracting matched phrases"
linkTitle: "Extracting matched phrases"
weight: 1
description: "`ISearchResults<T>` provides methods that allow you to combine the original source text with the search results and extract the matched phrases."
---

## `CreateMatchPhrasesAsync` and `CreateMatchPhrases`

The intent of the `ISearchResults<T>.CreateMatchPhrasesAsync` methods is to allow you to provide a mechanism by which
LIFTI can retrieve the original source text, from items or loosely indexed text, and automatically
extract phrases for the matched token locations. Where a multiple tokens are matched in a sequence, they will be combined into 
a single phrase.

For the `CreateMatchPhrasesAsync<TItem>` overloads that create the matched phrases from an indexed item of type `TItem`,
you can either fetch the items one at a time, or be provided with a list of item ids and fetch all the relevant items
in bulk. The latter is more efficient if you are having to go to an external source for the data, e.g. using a database
query.

### Example

This example demonstrates searching against an index of `Book` items:

``` csharp
public class Book
{
    public int BookId { get; set; }
    public string Title { get; set; }
    public string[] Authors { get; set; }
    public string Synopsis { get; set; }
}

var bookIndex = new FullTextIndexBuilder<int>() // Books are indexed by their BookId property, which is an int.
    .WithObjectTokenization<Book>(
        options => options
            .WithKey(b => b.BookId)
            .WithField("Title", b => b.Title, tokenOptions => tokenOptions.WithStemming())
            .WithField("Authors", b => b.Authors)
            .WithField("Synopsis", b => b.Synopsis, tokenOptions => tokenOptions.WithStemming()))
    .Build();

var results = bookIndex.Search("first | novel");
```

And then printing the matched phrases from each of the fields to the console window:

``` csharp
foreach (var result in await results.CreateMatchPhrasesAsync(i => books.First(x => x.BookId == i)))
{
    Console.WriteLine($"{result.SearchResult.Key} ({result.SearchResult.Score})");

    foreach (var fieldPhrase in result.FieldPhrases)
    {
        Console.Write($"  {fieldPhrase.FoundIn}: ");
        Console.WriteLine(string.Join(", ", fieldPhrase.Phrases.Select(x => $"\"{x}\"")));
    }
}
```


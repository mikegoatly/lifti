---
title: "Indexing Objects"
linkTitle: "Indexing Objects"
date: 2020-07-14
description: >
  You're not restricted to indexing strings against ids - you can also configure
  an index to extract text out of one or more properties of an object. Each
  configured property is a *field*.
---

Given a class called `Book`:

``` csharp
public class Book
{
    public int BookId { get; set; }
    public string Title { get; set; }
    public string[] Authors { get; set; }
    public string Content { get; set; }
}
```

Where you want users to be able to search for text in all three Title, Abstract and Content fields, you can build an index as:

``` csharp
// Books are indexed by their BookId property, which is an int.
var bookIndex = new FullTextIndexBuilder<int>()
    .WithItemTokenization<Book>(
        itemOptions => itemOptions
            .WithKey(b => b.BookId)
            .WithField("Title", b => b.Title,
                tokenOptions => tokenOptions.WithStemming())
            .WithField("Authors", b => b.Authors)
            .WithField("Synopsis", b => b.Synopsis,
                tokenOptions => tokenOptions.WithStemming()))
    .Build();
```

Indexing a set of books is as easy as:

``` csharp
books = new[]
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
}

await bookIndex.AddRangeAsync(books);
```

When you get search results back, they will be against the key stored in the index, i.e. the book's id:

``` csharp
// Both books contain "first" - prints "Matched items: 1, 2 with respective scores 0.274884808704732, 0.265418822719626"
var results = bookIndex.Search("first");
Console.WriteLine(
    "Matched items: " + 
    string.Join(", ", results.Select(i => i.Key)) +
    " with respective scores: " +
    string.Join(", ", results.Select(i => i.Score)));

// Only first book contains "the" in the title - prints "Matched items: 1"
results = bookIndex.Search("title=the");
Console.WriteLine("Matched items: " + string.Join(", ", results.Select(i => i.Key)));
```

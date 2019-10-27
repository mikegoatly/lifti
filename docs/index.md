# Introduction to LIFTI

LIFTI is a simple to use netstandard2 compatible in-memory full text indexing API.

``` c#
// Create a full text index with default settings
var index = new FullTextIndexBuilder<string>().Build();
            
// Index
index.Add("A", "This is some text associated with A: fizz");
index.Add("B", "Some buzz text for B");
index.Add("C", "Text associated with C is both fizz and buzz");

// Search for text containing both Fizz *and* Buzz
var results = index.Search("Fizz Buzz").ToList();

// Output: Items with both Fizz and Buzz: 1
Console.WriteLine($"Items with both Fizz and Buzz: {results.Count}");

// Search for text containing both Fizz *or* Buzz
results = index.Search("Fizz | Buzz").ToList();

// Outputs: Items with Fizz or Buzz: 3
Console.WriteLine($"Items with Fizz or Buzz: {results.Count}");
```

## Why choose LIFTI?

If you are building an application that refers to objects that contain lots of text, and you:

a) Don't want to store all the text in memory all the time (e.g. files or other text-based resources)
b) Want to be able to search the contents of the text quickly

Then LIFTI might be for you. It works best in client-based applications, e.g. applications built using:

* Client-side Blazor
* UWP
* Xamarin
* WPF

Though technically you can use it anywhere. For example, you might want be building an ASP.NET Core application
and you want to ensure that certain words are never used in user input. An in-memory index of exclusion words
could easily be used to do this.

## History

The original version of LIFTI was written in 2010 and hosted on [CodePlex](https://archive.codeplex.com/?p=lifti)
and evolved to become quite complicated, including automatic persistance to a backing file and support for
distributed transactions.

Support for netstandard1.3 was added by directly porting it and stripping out incompatible parts, primarily to
enable use in a personal UWP project ([Chordle](https://chordle.com)). This became the beta version of the LIFTI
package, and version 1.0.0 of the Lifti.Core package that was largely untouched for years.

This new version is a re-write in netstandard2 trying to re-focus on keeping it simple.
---
title: LIFTI - Using The IndexNavigator
permalink: /searching/index-navigator/
---

## The IndexNavigator class

When processing a search query, LIFTI leverages a class called `IndexNavigator` which allows for a character-by-character
navigation of the index.

You can use an `IndexNavigator` to do the same thing yourself. The example below demonstrates the following methods:

* `IIndexNavigator.Process` Navigates (forward only) the nodes in the index.
* `IIndexNavigator.GetExactMatches` Gets all the matches at the current location in the index. 
* `IIndexNavigator.GetExactAndChildMatches` Gets the exact matches *and* any matches in subsequent child nodes. This is the equivalent to a wildcard search.
* `IIndexNavigator.EnumerateIndexedTokens` Enumerates the words (tokens) that were indexed under the current location. This essentaially allows for a reverse-engineering of words stored in the index, albeit in their index normalized form.

``` csharp
public static async Task RunAsync()
{
    // Create a full text index with default settings
    var index = new FullTextIndexBuilder<string>().Build();

    // Index some sample data
    await index.AddAsync("Item1", "Catastrophe");
    await index.AddAsync("Item2", "Casualty");
    await index.AddAsync("Item3", "Cat");

    // To programatically search the index, create an index navigator instance 
    // from the index snapshot.
    using (var navigator = index.CreateNavigator())
    {
        // Navigate through the letters 'C' and 'A' (these will be the characters in their 
        // *index normalized* form)
        navigator.Process("CA".AsSpan());

        // There will be no exact matches at the current position in the index, but 3 matches 
        // when considering child matches, i.e. words starting with "ca"
        // Writes: Exact matches: 0 Exact and child matches: 3
        WriteMatchState(navigator);

        // Navigating through the 'T' of Catastrophe and Cat, but not Casualty
        navigator.Process('T');

        // Writes: Exact matches: 1 Exact and child matches: 2
        WriteMatchState(navigator);

        // Use EnumerateIndexedTokens to reverse-engineer the words that have been indexed
        // under the current location in the index, in their normalized form.
        // Writes:
        // CAT
        // CATASTROPHE
        foreach (var token in navigator.EnumerateIndexedTokens())
        {
            Console.WriteLine(token);
        }

        // The Process method returns true if navigation was successful, and false otherwise:
        // Writes: True
        Console.WriteLine(navigator.Process('A'));

        // Writes: False
        Console.WriteLine(navigator.Process("ZOOOOM"));
    }
}

public static void WriteMatchState(IIndexNavigator navigator)
{
    Console.WriteLine($@"Exact matches: {navigator.GetExactMatches().Matches.Count} 
Exact and child matches: {navigator.GetExactAndChildMatches().Matches.Count}");
}
```

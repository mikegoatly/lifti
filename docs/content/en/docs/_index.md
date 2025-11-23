
---
title: "LIFTI"
linkTitle: "Documentation"
weight: 20
menu:
  main:
    weight: 20
---

LIFTI is a simple to use .NET in-memory full text indexing library.

If you are building an application that refers to objects that contain lots of text, and you:

<ol type="a">
<li>Don't want to store all the text in memory all the time (e.g. files or other text-based resources)</li>
<li>Want to be able to search the contents of the text quickly</li>
</ol>

Then LIFTI is for you. You could use it in:

* Client applications, e.g. Blazor, UWP, Xamarin, WPF, Uno Platform, Avalonia UI
* ASP.NET applications where you need to perform a fast search against a long list of words. An in-memory index of exclusion words
could easily be used to do this.
* Lots of other scenarios!

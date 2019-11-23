# Why choose LIFTI

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

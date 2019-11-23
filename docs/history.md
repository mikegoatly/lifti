# History

The original version of LIFTI was written in 2010 and hosted on [CodePlex](https://archive.codeplex.com/?p=lifti)
and evolved to become quite complicated, including automatic persistance to a backing file and support for
distributed transactions.

Support for netstandard1.3 was added by directly porting it and stripping out incompatible parts, primarily to
enable use in a personal UWP project ([Chordle](https://chordle.com)). This became the beta version of the LIFTI
package, and version 1.0.0 of the Lifti.Core package that was largely untouched for years.

This new version is a re-write in netstandard2 trying to re-focus on keeping it simple.

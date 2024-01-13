---
title: "Custom stemmers"
linkTitle: "Custom stemmers"
weight: 7
description: >
  You can implement a custom stemmer if the default English Porter stemmer doesn't meet your needs.
---

Let's say that for some reason you needed to stem every indexed token so that it was at most 3 characters long:

```csharp
public class FirstThreeLettersStemmer : IStemmer
{
    public bool RequiresCaseInsensitivity => false;

    public bool RequiresAccentInsensitivity => false;

    public void Stem(StringBuilder builder)
    {
        if (builder.Length > 3)
        {
            builder.Length = 3;
        }
    }
}
```

`RequiresCaseInsensitivity` and `RequiresAccentInsensitivity` are hints used by the index at creation time that force it to enable
case/accent sensitivity.  Case insensitivity means that any text passed to your stemmer will already be uppercase. Accent insensitivity means 
that accents will automatically be stripped prior to being sent to the stemmer.

Once you've got your stemmer implemented, you just need to give it to the `FullTextIndexBuilder`:

``` csharp
var index = new FullTextIndexBuilder<int>()
    .WithDefaultTokenization(o => o.WithStemming(new FirstThreeLettersStemmer()))
    .Build();
```

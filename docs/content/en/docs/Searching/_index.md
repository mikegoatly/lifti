---
title: "Searching"
linkTitle: "Searching"
weight: 5
description: >
  The LIFTI query syntax
---


## Quick examples

Example|Meaning
-|-
West|**West** must appear in the text.
?Wst|Words that [fuzzy match](#fuzzy-matching) with **wst** must appear in the text.
title=West|**West** must appear in the ***title*** field of an indexed object.
doc*|Words that starts with **doc**ument must appear in the text. [See wildcard matching](#wildcard-matching)
%%ing|Words that starts with any two letters and end with **ing**, e.g. *doing*. [See wildcard matching](#wildcard-matching)
west&nbsp;&&nbsp;wing|The words **west** and **wing** must appear in the text.
west&nbsp;wing|The words **west** and **wing** must appear in the text - the default operator is & if none is specified between search words.
west&nbsp;\|&nbsp;wing|The words **west** or **wing** must appear in the text.
west&nbsp;~&nbsp;wing|**west** and **wing** must appear near to each other (within 5 words - the default) in the text.
west&nbsp;~3&nbsp;wing|**west** and **wing** must appear near to each other (within **3** words) in the text.
west&nbsp;~>&nbsp;wing|**west** must be followed by **wing** closely (within 5 words - the default) in the text.
west&nbsp;~3>&nbsp;wing|**west** must be followed by **wing** closely (within **3** words) in the text.
west&nbsp;>&nbsp;wing|**west** must precede **wing** anywhere in the text
"the&nbsp;west&nbsp;wing"|The words **the west wing** must appear in sequence in the indexed text.
"notr\*&nbsp;dam\*"|A word starting with notr must be immediately followed by a word starting with dam, e.g. **Notre Dame**.

Search terms can be combined and placed in parenthesis:

Example|Meaning
-|-
"west wing" ~ "oval office"|**West wing** must appear near **Oval Office**

### Fuzzy matching

LIFTI uses [Levenshtein distance](https://en.wikipedia.org/wiki/Levenshtein_distance) to perform fuzzy matches between a search term and tokens in the index.
The distance between two words is the number of edits that are required to match them, including:

* insertions: fid would match fi**n**d 
* deletions: foood would match food
* substitutions: frnd would match f**i**nd
* transpositions: fnid would match f**in**d - Transpositions are a special case, because although two characters are affected, it is considered a single edit.

The resulting Levenshtein distance between any matched term and the search term is used to reduce the score of the match. This means that documents containing
words that are closer matches will typically be surfaced higher up in the search results.

To prevent a [combinatorial explosion](https://en.wikipedia.org/wiki/Combinatorial_explosion) of potential matches, LIFTI currently limits the maximum number
of allowed edits to 3, and sequential edits to 1. This means that as of now:

* **feed** will *not* match **food** because it requires two sequential edits
* **redy** will *not* match **friendly** because it requires 4 insertions

### Defaulting search terms to fuzzy matching

By default LIFTI will treat a search term as an exact match, however [you can configure the index](../index-construction/withqueryparser/#configuring-the-default-lifti-queryparser) so that any search term (apart from those containing wildcards)
will be treated as a fuzzy match.

### Wildcard matching

Any search term containing `*` or `%` will be considered a wildcard match, where:

* `*` matches zero or more characters
* `%` matches any single character

## Query Operators

### Basic word matches

Any text in a query will be tokenized using to the provided tokenizer, 
enforcing the same word stemming, case/accent sensitivity rules as used in the index.

---

### Starts with (`*`)

Adding a `*` to the end of a word searches for words starting with the given text.

`foo*` would match occurrences of `food` and `football`

---

### And (`&`)

The and operator (`&`) Performs an intersection of two intermediate query results, combining word positions for successful matches.

`Food & Burger` searches for items containing both `"food"` and `"burger"` at any position, and in any field.

(Alternatively `Food Burger` will have the same effect as the default operator between query parts is an `&`.)

---

### Or (`|`)

Performs a union of two intermediate query results. Where an items appears in both sets, word positions are combined into one list.

Restricts results to same field by default: **false**

---

### Bracketing expressions

Brackets can be used to group expressions together.

e.g. `(food & cake) | (cheese & biscuit)`

---

### Field restrictions (`field=...`)

These allow for restricting searches within a given field.

`title=analysis | body=(chocolate & cake)` Searches for items with `"analysis"` in the title field *or both* `"chocolate"` and `"cake"` in the body field.

`title=analysis food` Searches for items with `"analysis"` in the title field *and* `"food"` in *any* field.

---

### Sequential text (`"..."`)

Placing quotes around a search phrase will enforce that the words all appear
immediately next to each other in the source text.

`"cheese burger"` will only match items that have text containing `"cheese"` followed immediately by `"burger"`.

---

### Near (`~` and `~n`)

The near operator performs a positional intersection of two results based on the position of the word in a field.

The `~` operator requires that words must be within 5 words of one another. This can value can be controlled by specifying a number, e.g. `~4` to restrict to only returning results within 4 words of one another.

`cheese ~ cake` will return items containing the words `"cheese"` and `"cake"` in either order, up to 5 words apart, e.g. `"the cake was made with cheese"` and `"I like cheese and cake"` would both match, but `"cake is never to be considered a substitute for cheese"` would not.

---

### Near following (`~>` and `~n>`)

Same as Near (`~`) except that order is important in the positional intersection.

`cheese ~> cake` will match `"cheese and cake"` but not `"cake and cheese"`

---

### Following (`>`)

Same as Near Following (`~>`) except there are no constraints on how far apart the words can be.

`cheese > cake` will match any text where `"cheese"` precedes `"cake"` in a given field.

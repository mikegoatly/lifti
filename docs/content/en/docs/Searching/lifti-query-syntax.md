---
title: "The LIFTI Query Syntax"
linkTitle: "The LIFTI Query Syntax"
weight: 5
description: >
  The LIFTI query syntax
---

## Quick examples

Example|Meaning
-|-
West|**West** must appear in the text [exactly](#exact-word-matches).
?Wst|Words that [fuzzy match](#fuzzy-match-) with **wst** must appear in the text.
title=West|A [field restricted](#field-restrictions-field) search. **West** must appear in the ***title*** field of an indexed object.
doc*|Words that starts with **doc**ument must appear in the text. [See wildcard matching](#wildcard-matching)
%%ing|Words that starts with any two letters and end with **ing**, e.g. *doing*. [See wildcard matching](#wildcard-matching)
west&nbsp;&&nbsp;wing|The words **west** [and](#and-) **wing** must appear in the text.
west&nbsp;wing|The words **west** [and](#and-) **wing** must appear in the text - the default operator is & if none is specified between search words.
west&nbsp;\|&nbsp;wing|The words **west** [or](#or-) **wing** must appear in the text.
west&nbsp;~&nbsp;wing|**west** and **wing** must appear [near to each other](#near--and-n) (within 5 words - the default) in the text.
west&nbsp;~3&nbsp;wing|**west** and **wing** must appear [near to each other](#near--and-n) (within **3** words) in the text.
west&nbsp;~>&nbsp;wing|**west** must be [followed by](#near-following--and-n) **wing** closely (within 5 words - the default) in the text.
west&nbsp;~3>&nbsp;wing|**west** must be [followed by](#near-following--and-n) **wing** closely (within **3** words) in the text.
west&nbsp;>&nbsp;wing|**west** must [precede](#following-) **wing** anywhere in the text
"the&nbsp;west&nbsp;wing"|The words **the west wing** must appear [in sequence](#sequential-text-) in the indexed text.
"notr\*&nbsp;dam\*"|You can use [wildcards](#wildcard-matching) and [fuzzy matching](#fuzzy-match-) in a [sequential text](#sequential-text-) query. In this case, a word starting with **notr** must be immediately followed by a word starting with **dam**, e.g. **Notre Dame**.

Search terms can be combined and placed in parenthesis:

Example|Meaning
-|-
"west wing" ~ "oval office"|**West wing** must appear near **Oval Office**
(west \| east) & wing|**west wing** or **east wing** must appear in the document.

## Query Operators

### Exact word matches

Any text in a query will be tokenized using to the provided tokenizer, 
enforcing the same word stemming, case/accent sensitivity rules as used in the index.

---

### Fuzzy match (`?`)

By prefixing a search term with `?` a fuzzy matching algorithm will be used to match the search term against the index.

#### Defaulting search terms to fuzzy matching

By default LIFTI will treat a search term as an [exact word match](#exact-word-matches), however [you can configure the index](../index-construction/withqueryparser/#configuring-the-default-lifti-queryparser) so that any search term (apart from those containing wildcards)
will be treated as a fuzzy match.

#### Fuzzy matching

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

---

### Wildcard matching

Any search term containing `*` or `%` will be considered a wildcard match, where:

* `*` matches zero or more characters
* `%` matches any single character. You can use multiple `%` in a row to indicate an exact number of characters that need to be matched.

Examples:

* `foo*` would match occurrences of `food` and `football`
* `*ing` would match `drifting` and `flying`
* `%%%ld` would match `could` and `mould` (but **not** `should`, because it has 4 letters before the `ld`)
* `%%p*` matches words starting with any two characters followed by `g`, then any zero or more characters, e.g. `map`, `caps`, `duped`

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

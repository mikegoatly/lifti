---
title: "The LIFTI Query Syntax"
linkTitle: "The LIFTI Query Syntax"
weight: 1
description: >
  The default query parser for an index makes use of a powerful query syntax.
---

> Don't want to use advanced queries? You'll want to configure the [simple query parser](../simple-queries).

## Quick examples

Example|Meaning
-|-
West|**West** must appear in the text [exactly](#exact-word-matches).
?Wst|Words that [fuzzy match](#fuzzy-match-) with **wst** must appear in the text.
?3,2?Wst|Words that [fuzzy match](#fuzzy-match-) with **wst** must appear in the text, with a specified max edit distance and max sequential edits.
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

By prefixing a search term with `?` a fuzzy matching algorithm will be used to match the search term against the index. You can optionally specify the maximum edit distance and maximum number of sequential edits
for a specific search term using the formats:

`?{max edits},{max sequential edits}?term`

For example `?2,1?food` will search for "food" with a maximum number of edits of 2, and maximum sequential edits of 1.

You can omit one or the other parameter if required, so `?2?food` will only set the maximum number of edits to 2, leaving the maximum sequential edits at the default value. If you want to only include
the maximum number of sequential edits, then you must include a leading comma in the parameter set, e.g. `?,2?food`

See [Fuzzy Matching](../fuzzy-matching) for more details.

#### Defaulting search terms to fuzzy matching

By default LIFTI will treat a search term as an [exact word match](#exact-word-matches), however [you can configure the index](../index-construction/withqueryparser/#configuring-the-default-lifti-queryparser) so that any search term (apart from those containing wildcards)
will be treated as a fuzzy match.

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

`Food & Burger` searches for documents containing both `"food"` and `"burger"` at any position, and in any field.

(Alternatively `Food Burger` will have the same effect as the default operator between query parts is an `&`.)

---

### Or (`|`)

Performs a union of two intermediate query results. Where a document appears in both sets, word positions are combined into one list.

Restricts results to same field by default: **false**

---

### Bracketing expressions

Brackets can be used to group expressions together.

e.g. `(food & cake) | (cheese & biscuit)`

---

### Field restrictions (`field=...`)

These allow for restricting searches within a given field.

`title=analysis | body=(chocolate & cake)` Searches for documents with `"analysis"` in the title field *or both* `"chocolate"` and `"cake"` in the body field.

`title=analysis food` Searches for documents with `"analysis"` in the title field *and* `"food"` in *any* field.

If your field name contains spaces or other special characters, you can escape it using square brackets `[` and `]`, e.g. `[my field]=chocolate`.

---

### Sequential text (`"..."`)

Placing quotes around a search phrase will enforce that the words all appear
immediately next to each other in the source text.

`"cheese burger"` will only match documents that have text containing `"cheese"` followed immediately by `"burger"`.

---

### Near (`~` and `~n`)

The near operator performs a positional intersection of two results based on the position of the word in a field.

The `~` operator requires that words must be within 5 words of one another. This can value can be controlled by specifying a number, e.g. `~4` to restrict to only returning results within 4 words of one another.

`cheese ~ cake` will return documents containing the words `"cheese"` and `"cake"` in either order, up to 5 words apart, e.g. `"the cake was made with cheese"` and `"I like cheese and cake"` would both match, but `"cake is never to be considered a substitute for cheese"` would not.

---

### Near following (`~>` and `~n>`)

Same as Near (`~`) except that order is important in the positional intersection.

`cheese ~> cake` will match `"cheese and cake"` but not `"cake and cheese"`

---

### Following (`>`)

Same as Near Following (`~>`) except there are no constraints on how far apart the words can be.

`cheese > cake` will match any text where `"cheese"` precedes `"cake"` in a given field.

## Escaping search text

Use a backslash `\` when you want to explicitly search for a character that clashes with the query syntax. For example, `A\=B` will search for a single token containing 
exactly "A=B", rather than attempting to perform a field restricted search.
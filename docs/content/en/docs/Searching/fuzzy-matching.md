---
title: "Fuzzy Matching"
linkTitle: "Fuzzy Matching"
weight: 3
description: >
  Fuzzy matching provides a mechanism by which you can search for words that are close to a search term, in terms of the number
  of differences between them.
---

Fuzzy matches can be explicitly searched for using the [LIFTI query syntax](lifti-query-syntax/#fuzzy-match-), or implied as the default for searches by 
[configuring the index](./index-construction/withqueryparser/#configuring-the-default-lifti-queryparser).

LIFTI uses [Levenshtein distance](https://en.wikipedia.org/wiki/Levenshtein_distance) to perform fuzzy matches between a search term and tokens in the index.
The distance between two words is the number of edits that are required to match them, including:

* insertions: fid would match fi**n**d 
* deletions: foood would match food
* substitutions: frnd would match f**i**nd
* transpositions: fnid would match f**in**d - Transpositions are a special case, because although two characters are affected, it is considered a single edit.

The resulting Levenshtein distance between any matched term and the search term is used to reduce the score of the match. This means that documents containing
words that are closer matches will typically be surfaced higher up in the search results.

### Configuration

To prevent a [combinatorial explosion](https://en.wikipedia.org/wiki/Combinatorial_explosion) of potential matches, LIFTI provides two control mechanisms for fuzzy matching:

* **Maximum number of edits** - the total number of edits that can be used in any potential match. The default for this value is calculated as `search term length/2`.
* **Maximum number of sequential edits** - the maximum number of edits that can be found sequentially in any potential match. The default for this value is calculated as `search term length/4`.

For example, with a max edits of **3** and max sequential edits of **1**:

* **feed** will *not* match **food** because it requires two sequential edits
* **redy** will *not* match **friendly** because it requires 4 insertions

Default values can be [configured at the index level](../index-construction/withqueryparser/#queryparserbuilder-options), and can either be expressed as a static value,
or a value calculated from the length of the search term.
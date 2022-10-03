---
title: "Simple Queries"
linkTitle: "Simple Queries"
weight: 4
description: >
  When you want to keep your queries simple, always searching for documents containing all (or just some) search terms,
  you can configure an index to use the simple query parser.
---

When you have [configured an index to use the simple query parser](../../index-construction/withsimplequeryparser) you can no longer
make use of the full LIFTI query syntax in your queries, however you can still configure the simple query parser to treat all search 
terms as [fuzzy matches](../fuzzy-matching).

Search terms are simply combined together with `and`s or `or`s, depending on your configuration, and punctuation is stripped out as per your 
index tokenization rules.


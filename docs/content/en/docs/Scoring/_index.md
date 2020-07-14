---
title: "Search Result Scoring"
linkTitle: "Result Scoring"
weight: 6
description: >
  How does LIFTI score results?
---

LIFTI uses a version of the [Okapi BM25](https://en.wikipedia.org/wiki/Okapi_BM25) algorithm to score search results. At the simplest level this means that search results will come back ordered by relevance.

Once nice feature of LIFTI is that in you also get each field scored independently. The overall score for a document is just a sum of these, but you could easily just re-order the results by one field over another should you so wish.
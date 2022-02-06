---
title: "Automatic Serialization"
linkTitle: "Automatic Serialization"
weight: 2
description: >
  With a bit of configuration you can configure the index to serialize to a backing
  file whenever changes are made to it.
---

First you will need to make sure that the index is deserialized before use, as demonstrated [here](..), and add an [index modification hook](../../index-construction/withindexmodificationaction) to serialize the index whenever a new snapshot is created.
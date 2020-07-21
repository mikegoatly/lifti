---
title: "Frequently Asked Questions"
linkTitle: "FAQ"
weight: 12
description: >
  Frequently asked questions about LIFTI
---

> This FAQ is a work in progress - as questions are asked, they will be added here for future reference.

## General

### Is LIFTI thread safe?

Yes, this is managed by two mechanisms:

1) **Single writer**: Locking is enforced so that there can only be one writer at any one time to the index.
2) **Snapshots**: Whenever changes are committed to the index, a read-only snapshot is taken. Queries are executed against the snapshots, so any modifications that occur during the processing of any given query will not affect them. This means that read operations are lock-free.

## Serializaton

### Can I automatically serialize an index when it changes?

Yes, you need to add a hook to `FullTextIndexBuilder<TKey>.WithIndexModificationAction`. There's an [example here](../Index%20construction/WithIndexModificationAction).

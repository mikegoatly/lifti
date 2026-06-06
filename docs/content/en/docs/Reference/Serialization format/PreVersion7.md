---
title: "V6 Serialization File Format"
linkTitle: "V6 Serialization File Format"
date: 2024-01-14
description: >
    Documentation for older serialization formats.
---

## Version 6 (v6.0.0)

![LIFTI Serialization Format](../../../../images/v6-serialization.svg)

- New in version 6 is the storage of object type and scoring metadata information for a document in the index, including an internal object type id it was extracted for, freshness date and scoring magnitude, if applicable.

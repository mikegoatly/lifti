---
title: "Serialization File Format"
linkTitle: "Serialization File Format"
date: 2024-01-14
description: >
    The current serialization format is version 6. 
---

## Version 6 (v6.0.0)

![LIFTI Serialization Format](../../../images/v6-serialization.svg)

Notes:

- Versions 2 to 5 are readable as a one-time conversion but always written back as version 6.
- Int32s are written as *positive* values using 7-bit encoding. This means that the maximum value is 2,147,483,647, apart from Int32s written by the `IntFormatterKeySerializer` which can't make the assumption that the value is always positive. For these, values are written using zig-zag encoding.
- New in version 6 is the storage of object type and scoring metadata information for a document in the index, including an internal object type id it was extracted for, freshness date and scoring magnitude, if applicable.
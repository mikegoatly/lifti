---
title: "Serialization File Format"
linkTitle: "Serialization File Format"
date: 2024-01-14
description: >
    The current serialization format is version 7.
---

## Version 7 (v7.0.0)

![LIFTI Serialization Format](../../../images/v7-serialization.svg)

Notes:

- Versions 2 to 6 are readable as a one-time conversion but always written back as version 7.
- Non-negative Int32s are written using 7-bit encoding. This means that the maximum value is 2,147,483,647, apart from Int32s written by the `IntFormatterKeySerializer` and signed Int32 values such as field `LastTokenIndex` values. These are written using zig-zag encoding.
- New in version 7 is the storage of per-field `LastTokenIndex` values alongside token counts in document field statistics. This supports exact-match queries at field boundaries without having to infer the last token index when an index is deserialized.

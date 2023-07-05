---
title: "Serialization File Format"
linkTitle: "Serialization File Format"
date: 2023-07-04
description: >
    The current serialization format is version 5. 
---

## Version 5 (v5.0.0)

![LIFTI Serialization Format](../../../images/v5-serialization.svg)

Notes:

- Versions 2 to 4 are readable as a one-time conversion but always written back as version 5.
- Int32s are written as *positive* values using 7-bit encoding. This means that the maximum value is 2,147,483,647, apart from Int32s written by the `IntFormatterKeySerializer` which can't make the assumption that the value is always positive. For these, values are written using zig-zag encoding.
- New in version 5 is the list of fields in the index. This is used upon deserialization to rehydrate the dynamic fields and ensure that the field names in the index being deserialized into are mapped correctly to fields in the serialized index.
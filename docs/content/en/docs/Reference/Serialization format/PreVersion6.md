---
title: "V5 Serialization File Format"
linkTitle: "V5 Serialization File Format"
date: 2024-01-14
description: >
    Documentation for older serialization formats.
---

## Version 5 (v5.0.0)

![LIFTI Serialization Format](../../../../images/v5-serialization.svg)

- New in version 5 is the list of fields in the index. This is used upon deserialization to rehydrate the dynamic fields and ensure that the field names in the index being deserialized into are mapped correctly to fields in the serialized index.
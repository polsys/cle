# `Cle.Common`

This library provides some helper types and interfaces between assemblies. The surface area should be kept as minimal as possible.

## Main points of interest:
- `Diagnostic*` types are used for passing errors and warnings in compiled code.
- `TextPosition` represents a point in a source file. This struct contains both an UTF-8 byte offset and a user-readable position.

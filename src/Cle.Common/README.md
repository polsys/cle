# `Cle.Common`

This library provides some helper types and interfaces between assemblies. The surface area should be kept as minimal as possible.

The type system of Clé lives in this assembly as type information is needed both in semantic compilation and code generation,
but the information would be too general to live in `Cle.SemanticAnalysis`.

## Main points of interest:
- `Diagnostic*` types are used for passing errors and warnings in compiled code.
- `TextPosition` represents a point in a source file. This struct contains both an UTF-8 byte offset and a user-readable position.
- `TypeDefinition` is the base class for all types, both language-defined and user-defined.
  - `SimpleType` implements basic types like integers and booleans.
  - `ConstantValue` is used for initialization of values.

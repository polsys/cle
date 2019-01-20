# Types

_The type system in Clé 0.1 is very simple and will be extended a lot in future releases._


## Simple types

These types are built into the language.
They are always passed by value.
For the operators defined for the types, see the section [Expressions](expressions.md).

### Integer types
There is currently one integer type: `int32`.
It is a 32-bit signed two's complement integer, with range from `-2^31` to `2^31-1` inclusive.


### Boolean type
The `bool` type has two possible values: `true` and `false`.
The internal representation of the type is currently unspecified.


### Void type
The type `void` is only used for indicating that a method does not return a value.
It is not a valid type in any other context.

# Expressions

## Types of expressions

### Literals
A Boolean literal has the value `true` or `false`.
They are always condidered to be type `bool`.

Integer literals are unsigned decimal numbers.
The type of the expression depends on the value of the literal:
- `int32` for values between `0` and `2^31-1` inclusive,
- `uint32` for values between `2^31` and `2^32-1` inclusive,
- **unimplemented** otherwise.

Note that negative numbers are interpreted as unary minus applied to an integer literal.


### Method calls
A method call is of form
```
method_name ( [expression] [, expression]* )
```
The method name is resolved according to standard name resolution rules.
Each parameter expression is evaluated and the type checked to match the callee declaration.
The type of the expression is the return type of the callee.

If the name resolution finds multiple matching methods, an error occurs.


### Variable references
A variable reference consists of the variable name, not followed by parentheses such as in a method call.
The variable is resolved in the current scope, and the expression has the value and type of the variable.


### Parentheses
Any expression wrapped in parentheses is an expression.


### Unary expressions
```
unary_operator expression
```
where `unary_operator` is one of `-`, `!`, or `~`.
- `-x` subtracts integral `x` from `0`.
  The type of the result is `int32` unless `x` cannot be represented as `int32`, in which case the result is `int64`.
  If `x` is the smallest representable value for the result type, this operation does nothing.
  As an important corner case, `-2147483648` is considered `int32` even though `2147483648` cannot be represented as `int32`.
  The only allowed `uint64` value to negate is `9223372036854775808`, to permit representing the smallest `int64` value.
- `!x` logically negates Boolean `x` and is of type `bool`.
- `~x` bitwise negates integral `x` and has the same type as `x`.


### Binary expressions
```
expression binary_operator expression
```
where the left expression is evaluated before the right expression.
The following operators accept integer operands and are of integer type:
- `+`, arithmetic addition.
- `-`, arithmetic subtraction.
- `*`, arithmetic multiplication.
- `/`, arithmetic division.
  The right-hand expression must not be equal to zero, or a compile- or run-time error occurs.
  The division always rounds toward zero.
- `%`, arithmetic remainder.
  The result has the same sign as the left operand.
  The right-hand expression must not equal zero.
  Additionally, it may not equal `-1` when the left-hand expression is equal to the smallest representable `int32`/`int64`.
  In either case, the error behavior is the same as with division.
- `&`, bitwise AND.
- `|`, bitwise OR.
- `^`, bitwise XOR.
- `<<`, shift left.
- `>>`, shift right.
  The left operand is shifted left/right by the number of bits specified by the right operand.
  The shift amount is between `0` and `31` bits for `int32` values, with out-of-bounds values wrapping around.

The following operators accept `bool` operands and are of type `bool`:
- `&`, logical AND.
- `|`, logical OR.
- `^`, logical XOR.

The comparison operators `<`, `<=`, `=>` and `>` accept integer values and are of type `bool`.

The comparison operators `==` and `!=` accept both integer and Boolean values and are of type `bool`.

**Work in progress:** The exact types of integer operators for non-`int32` operands are yet undefined.


## Associativity and precedence
All binary operators are left associative.
The operators have the following precedence, from highest (evaluated first) to lowest (evaluated last):
```
unary - ! ~
* / %
+ -
<< >>
== != < <= >= >
& | ^
```
For example, `2 - 1 * 3 == -1 & true` is evaluated as `((2 - (1 * 3)) == -(1)) & true`.
The precedence may be modified with parentheses.

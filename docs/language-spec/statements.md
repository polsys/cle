# Statements

Statements are the building blocks of Clé programs.
Each statement is an action.
In contrast with expressions, they do not produce values and they appear standalone.

There are several kinds of statements, described in subsection below.
Some statements end in a semicolon while blocks and statements with a block end in a closing brace.


## Block
A block is a collection of statements (including nested blocks) executed in order from top to bottom.
Every method contains a block as its top-level statement.
A block begins with a `{`, contains arbitrarily many statements (including none) and ends in a `}`.

A block is a scope for local variables.
Local variables defined in a block are only accessible in that block and any nested blocks.


## Variable definition
A variable definition is of form
```
type_name variable_name = initial_value ;
```

The type name is a language-defined, simple or full name that must be visible in the current file.
The variable name is a simple name that must not already be used as a variable name in the current or any enclosing block.
The initial value is an expression that produces a value of the variable type.

A variable cannot be accessed before its definition.
When used in an expression, the variable produces a value of its defined type.

**Note:** Currently, the following code compiles:
```
{
    {
        int32 v = 1;
    }
    int32 v = 2;
}
```
However, in a future language version the declaration of `v` in the outer block may be extended to span the whole block (without being accessible before the definition).
In that case, the inner declaration of `v` violates the uniqueness rule.
You are advised to avoid this construct.


## Assignment
An assignment is of the form
```
variable_name = value ;
```

The variable name must be defined in the current or enclosing block before the assignment statement.
The value is an expression that must have the same type as the variable.

As can be expected, this statement assigns the value to the variable.


## Method call
A standalone method call is a method call expression in statement context.
In contrast to being part of an expression, the return value of the called method is ignored.


## Return
A return statement is either of the form `return;` or of the form `return value;`.
The first case may be used in a `void`-returning method.
In the latter case, the value must be an expression of the method return type.

When executed, this statement exits the current method and returns the return value to the caller.


## If
There are two kinds of conditional statements.
The first case if of form
```
if ( condition ) block
```

The condition is an expression of type `bool`.
If the expression is evaluated as true, the block is executed.
Otherwise, the statement following the block is executed.

**Note:** In Clé, other kinds of statements cannot be used in place of the block.

The second form of `if` statement has an `else` branch following the block:
```
if ( condition )
block
else block_or_if
```

Here, `block_or_if` is either a block or another if statement (of either form).
It is executed if the condition is false.


# While
The `while` statement consists of a condition and a body:
```
while ( condition ) block
```

The condition is a `bool` expression.
If it is evaluated as true, the block is executed.
After the block is executed, the condition is evaluated again.
When the condition is false, execution jumps to the statement following the block.


# Return checking
Non-`void` methods are required to return a value.
The following rules specify whether a statement returns:
- A `return` statement returns.
- An `if` statement returns if it has an `else` branch and both the block and the else branch return.
- A block returns if an enclosed statement returns. Statements following the returning statement are considered unreachable.
- Other statements never return.

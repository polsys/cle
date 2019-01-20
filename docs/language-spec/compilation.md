# Compilation and modules

Clé programs are composed of one or more modules.
Compiling a Clé program means recursively compiling the dependencies of the main module, and then the main module itself.
The output is a single executable file, and optionally debugging symbols and other output.


## Modules
The module system will be fleshed out in a future version of the language.

As of 0.1, a module is a collection of source files in a single directory (not including subdirectories).
The source files within a module are compiled in an arbitrary order.

The main module is the module explicitly specified at compiler invocation.
The main module must contain exactly one method marked with the `[EntryPoint]` attribute.


## Source files
Source files are UTF-8 encoded text files with the extension `.cle`.
They are composed of tokens (identifiers, symbols, literals) delimited by whitespace (horizontal or vertical).

A source file must begin with a namespace declaration:
```
namespace namespace_name;
```
Then, the file may contain any number of method definitions.
All method definitions have an associated visibility modifier, described in the next section.
All items have namespace equal to the declared file namespace.
There is no restriction on the order of methods: a method may refer to another method defined later in the source file.

Additionally, comments may appear at any point in the source file.
They begin with `//` and continue until the end of line.
The initial `//` and all comment contents are interpreted as whitespace.


### Visibility modifiers
Each source file item is annotated with a visibility modifier that specifies where it can be accessed.
There are three visibility classes:
- `private` items are only visible in the declaring file. The name may be reused in other files.
- `internal` items are only visible in the declaring module. The name must be unique within the module.
- `public` items are visible in the declaring module and all the modules that reference the declaring module.


## Identifiers and namespaces
Each function has a _full name_ of the form `namespace_name::simple_name`.
This uniquely identifies the function within the declaration scope, subject to the visibility rules above.
It is an error to declare a function with a name that is already
- used by a function defined in the same file,
- used by an `internal` or `public` function in the same module, or
- used by a `public` function in a referenced module.

Simple names must conform to the following naming rules:
- They may contain the letters `a`-`z` and `A`-`Z`, the underscore `_` and the decimal digits `0`-`9`.
- They may not start with a decimal digit.
- They may not consist of a single underscore only.
- They may not be equal to a keyword or a reserved type name (such as `int32`).

Namespace names follow the same rules as simple names, with two exceptions:
- They may contain several parts delimited by `::`. The name may not start or end in `::`.
- The name or its part may be equal to a reserved type name. A part, but not the whole name, may be equal to a keyword.

An item may be referred to by its _simple name_ or _full name_.
If the name does not contain a namespace prefix, it is a simple name.
In that case, its namespace is assumed to be equal to the namespace declared at the start of the source file.
The namespace prefix must be complete; for example, the function `Foo::Bar::Baz` cannot be referenced as `Bar::Baz` even if the file namespace is `Foo`.

It is possible that the same name is defined in two referenced modules.
In that case, an error cannot be raised at declaration compilation time.
Instead, an ambiguity error is raised when the name is referred to.

All names are case-sensitive.


## Methods
A method definition has the following syntax:
```
attributes
visibility_modifier return_type method_name (parameter_list)
block
```

The visibility modifier is discussed above.
The return type is a type name (language-defined, simple or full name) visible in the current scope.
The method name is a simple name unique within the scope and visible scopes.
The full name of the method is a combination of the file namespace name and the declared method name.

The definition may be preceded by any number of attributes.
These are discussed in [Attributes](attributes.md).

The parameter list is a comma-separated list of variable definitions.
These definitions only consist of the variable type and name and may not specify an initial value.
The parameters are local variables initialized to values specified at the method call site.

The _block statement_ is discussed in [Statements](statements.md).
The block in a non-`void` method must return a value with a `return` statement.
In a `void` method, the compiler will insert an implicit return statement at the end of the block if required.

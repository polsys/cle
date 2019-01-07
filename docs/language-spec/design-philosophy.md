# The Clé design philosophy

## Simplicity
The first and foremost design rule of Clé is simplicity.
To keep the project interesting and alive, it has to be broken into manageable pieces.
Whenever a language feature would require complex compiler machinery, it will be left out for later.
Good progress on small features is better than little progress on large features.

_This is why Clé 0.1 does not support the `break` keyword in loops or `&&` in expressions._


## Clarity
The language should be unambiguous to parse, both by human and by computer.
The syntax and semantics is therefore guided more by C# and Rust.
For example, everything is namespaced.
Explicit is better than implicit.

_This, combined with simplicity, is why Clé 0.N won't support implicit conversions._


## Native code
There is much more to learn in native code compilation than in an interpreted language.
This is not to say that interpreted languages are bad - far from it - but rather that they are outside the scope of this project.

_Combined with simplicity, this means that the language is procedural in style and has no fancy features like type polymorphism or garbage collection._


## Safety
All variable lifetimes are managed by the compiler and breaking the guarantees should be impossible.
There are enough footguns in this world.

_Combined with the previous point and the fact that this is a hobby project, this rule is sure to be accidentally broken many times. Don't code anything critical._


## And since you asked...
Four spaces for indentation.

Braces on their own line.

Functions, global constants and user-defined types are `UpperCamelCase`, variables and parameters `lowerCamelCase`.
Namespaces are `UpperCamelCase` unless extending a language-defined type.

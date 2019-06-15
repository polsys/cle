# Attributes

Attributes are special instructions to the compiler.
One or more attributes may be applied to a method to affect how the method is handled.
An attribute has the syntax
```
[attribute_name parameter_list]
```
where `parameter_list` is an optional parenthesized list of comma-separated values.
Not all attributes have parameter lists.


## `[EntryPoint]`
This attribute must be applied to exactly one method in the main module.
The method must take no parameters and return `int32`.

The method marked with this attribute is called when the program is run.
When the method returns, the program quits with return code equal to the returned value.

This attribute is ignored in modules other than the main module.


## `[Import(name, library)]`
This attribute indicates that the method is dynamically imported (e.g. from a DLL file) using the target operating system mechanism.
The `name` and `library` parameters are string literals that specify the name and the containing library, respectively, of the function to import.
Both strings may only contain ASCII characters.

The method marked with this attribute must not have a method body.
The name of the method does not need to match the `name` parameter.

There is no difference between calling an imported method and an ordinary method.
However, passing anything else than simple types as parameters or return value requires special care, since the imported function may not be compatible with Clé type system or memory model.

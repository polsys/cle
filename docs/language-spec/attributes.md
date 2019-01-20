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

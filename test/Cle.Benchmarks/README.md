# `Cle.Benchmarks`

This assembly contains `BenchmarkDotNet` benchmarks for various parts of the compiler.
Some rough guidelines:

- A benchmark may exercise a single subsystem (e.g., the parser) or a component (e.g., the lexer, the register allocator).
- This assembly should not contain isolated microbenchmarks for single functions but rather an easy-to-run overview of the compiler.
- As usual, no benchmark should include setup overhead or touch the disk.

The application uses `BenchmarkSwitcher` functionality, which allows specifying the benchmarks to run from command line or interactively.


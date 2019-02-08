# `Cle.IntegrationTests`

This test assembly contains integration tests that exercise the full compiler.
Each test case is a self-contained module in `TestCases` directory.
The module is compiled and executed, and the output (return code, console) verified.

Types of test cases:
- `CodeGenBringUp` tests are very simple tests that exercise the compiler backend.
They provide a starting point for porting the compiler to a new architecture.
- TODO

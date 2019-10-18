# `Cle.CodeGeneration`

This assembly implements the native code generation phase.
It takes the IR produced by semantic analysis as its input.

The unit test coverage for this assembly is significantly lower than for others.
This is because the generated code (not to speak of the EXE format) is harder to verify
and there are very many valid outputs (e.g. due to register allocation decisions).
Instead, the `Cle.IntegrationTests` assembly is the main test of code generation.

## Main points of interest:
- `LoweringX64` transforms the intermediate representation into a lower-level form that knows platform conventions.
- `PeepholeOptimizer` improves the quality of lowered code significantly, simplifying lowering and helping the register allocator.
- `X64RegisterAllocator` implements a simple Linear Scan Register Allocator operating on SSA form code.
- `WindowsX64CodeGenerator` transforms the lowered and allocated code into machine code, taking platform conventions into account. It is the entry point of this assembly.
- `PortableExecutableWriter` implements the Windows PE (`.exe`) format.
- `X64Emitter` methods output x64 instructions.

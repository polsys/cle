# `Cle.SemanticAnalysis`

This assembly implements the semantic analysis phase.
It receives syntax trees and outputs intermediate code (IR) suitable for optimization or code generation.

The compilation happens in two phases:
1. All declarations are verified (`MethodCompiler.CompileDeclaration` for methods) and added to a central location (not in this assembly).
2. Method bodies are compiled in arbitrary order.
  - First, `MethodCompiler.CompileBody` transforms the syntax tree into IR. This IR is not in SSA form.
  - Then, `SsaConverter.ConvertToSsa` transforms the IR into SSA IR. Since the IR types are immutable, this ends up recreating all the data structures, but is easier to debug than on-the-fly conversion.

## Main points of interest
- `MethodCompiler` does the bulk of semantic analysis.
- `ExpressionCompiler` does the part related to expressions.
- `SsaConverter` implements the SSA conversion.
- Various `BasicBlock[...]` types represent and construct the IR.
- The output of phase 1 is `MethodDeclaration`, which contains type information only.
- The output of phase 2 is `CompiledMethod`, which combines IR code with local values (variables, constants, temporaries).

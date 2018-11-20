using System.Text;
using JetBrains.Annotations;

namespace Cle.SemanticAnalysis
{
    /// <summary>
    /// Provides methods for getting string representations of compiled methods.
    /// </summary>
    public static class MethodDisassembler
    {
        private const string Indent = "    ";

        /// <summary>
        /// Writes the type information and disassembled basic blocks of the method to the output string builder.
        /// </summary>
        public static void Disassemble([NotNull] CompiledMethod method, [NotNull] StringBuilder outputBuilder)
        {
            // Write the types and initial values of locals
            for (var i = 0; i < method.Values.Count; i++)
            {
                var local = method.Values[i];
                outputBuilder.AppendLine($"; #{i,-3} {local.Type.TypeName} = {local.InitialValue}");
            }

            // Then write the basic block graph
            DisassembleBody(method, outputBuilder);
        }

        /// <summary>
        /// Writes the disassembled basic blocks of the method to the output string builder.
        /// </summary>
        public static void DisassembleBody([NotNull] CompiledMethod method, [NotNull] StringBuilder outputBuilder)
        {
            for (var blockIndex = 0; blockIndex < method.Body.BasicBlocks.Count; blockIndex++)
            {
                var block = method.Body.BasicBlocks[blockIndex];

                // Basic block header
                outputBuilder.AppendLine("BB_" + blockIndex + ":");

                // Instructions
                foreach (var instruction in block.Instructions)
                {
                    // Indented opcode
                    outputBuilder.Append(Indent + instruction.Operation);

                    // Depending on the instruction, the parameters or just a newline
                    switch (instruction.Operation)
                    {
                        case Opcode.BranchIf:
                            outputBuilder.AppendLine(" #" + instruction.Left + " ==> BB_" + block.AlternativeSuccessor);
                            break;
                        case Opcode.Return:
                            outputBuilder.AppendLine(" #" + instruction.Left);
                            break;
                        default:
                            outputBuilder.AppendLine();
                            break;
                    }
                }

                // The successor block, if defined
                // However, do not create a line for trivial fallthrough
                if (block.DefaultSuccessor >= 0 && block.DefaultSuccessor != blockIndex + 1)
                {
                    outputBuilder.AppendLine(Indent + "==> BB_" + block.DefaultSuccessor);
                }

                // Empty line between basic blocks and after the last block
                outputBuilder.AppendLine();
            }
        }
    }
}

﻿using System.Text;
using Cle.SemanticAnalysis.IR;
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
                // The block may be null as the builder omits dead blocks but preserves numbering
                var block = method.Body.BasicBlocks[blockIndex];
                if (block is null)
                    continue;

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
                        case Opcode.Add:
                            AppendBinaryParameters(instruction, "+", outputBuilder);
                            break;
                        case Opcode.ArithmeticNegate:
                            outputBuilder.AppendLine($" #{instruction.Left} -> #{instruction.Destination}");
                            break;
                        case Opcode.BitwiseAnd:
                            AppendBinaryParameters(instruction, "&", outputBuilder);
                            break;
                        case Opcode.BitwiseNot:
                            outputBuilder.AppendLine($" #{instruction.Left} -> #{instruction.Destination}");
                            break;
                        case Opcode.BitwiseOr:
                            AppendBinaryParameters(instruction, "|", outputBuilder);
                            break;
                        case Opcode.BitwiseXor:
                            AppendBinaryParameters(instruction, "^", outputBuilder);
                            break;
                        case Opcode.BranchIf:
                            outputBuilder.AppendLine($" #{instruction.Left} ==> BB_{block.AlternativeSuccessor}");
                            break;
                        case Opcode.CopyValue:
                            outputBuilder.AppendLine($" #{instruction.Left} -> #{instruction.Destination}");
                            break;
                        case Opcode.Divide:
                            AppendBinaryParameters(instruction, "/", outputBuilder);
                            break;
                        case Opcode.Modulo:
                            AppendBinaryParameters(instruction, "%", outputBuilder);
                            break;
                        case Opcode.Multiply:
                            AppendBinaryParameters(instruction, "*", outputBuilder);
                            break;
                        case Opcode.Return:
                            outputBuilder.AppendLine($" #{instruction.Left}");
                            break;
                        case Opcode.ShiftLeft:
                            AppendBinaryParameters(instruction, "<<", outputBuilder);
                            break;
                        case Opcode.ShiftRight:
                            AppendBinaryParameters(instruction, ">>", outputBuilder);
                            break;
                        case Opcode.Subtract:
                            AppendBinaryParameters(instruction, "-", outputBuilder);
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
                    outputBuilder.AppendLine($"{Indent}==> BB_{block.DefaultSuccessor}");
                }

                // Empty line between basic blocks and after the last block
                outputBuilder.AppendLine();
            }
        }

        private static void AppendBinaryParameters(in Instruction instruction, string op, StringBuilder outputBuilder)
        {
            outputBuilder.AppendLine($" #{instruction.Left} {op} #{instruction.Right} -> #{instruction.Destination}");
        }
    }
}

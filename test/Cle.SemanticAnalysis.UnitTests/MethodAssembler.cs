using System;
using Cle.Common.TypeSystem;
using Cle.SemanticAnalysis.IR;
using JetBrains.Annotations;
using NUnit.Framework;

namespace Cle.SemanticAnalysis.UnitTests
{
    /// <summary>
    /// An UNRELIABLE intermediate representation assembler for TESTING ONLY.
    /// </summary>
    internal static class MethodAssembler
    {
        /// <summary>
        /// Creates a <see cref="CompiledMethod"/> out of the given IR disassembly,
        /// which must follow the <see cref="MethodDisassembler"/> output very closely.
        /// </summary>
        public static CompiledMethod Assemble([NotNull] string source, [NotNull] string fullName)
        {
            var method = new CompiledMethod(fullName);
            var graphBuilder = new BasicBlockGraphBuilder();
            BasicBlockBuilder currentBlockBuilder = null;
            
            var lines = source.Replace("\r\n", "\n").Split('\n');
            foreach (var rawLine in lines)
            {
                var currentLine = rawLine.Trim();

                if (currentLine.Length == 0)
                {
                    // Empty (or whitespace) line - skip
                }
                else if (currentLine.StartsWith("; #"))
                {
                    // A variable definition (hopefully) - add a local
                    ParseLocal(currentLine, method);
                }
                else if (currentLine.StartsWith("BB_"))
                {
                    // Starting a new basic block
                    // First finalize the current basic block, if needed, with a fallthrough
                    if (currentBlockBuilder != null && !currentBlockBuilder.HasDefinedExitBehavior)
                    {
                        currentBlockBuilder.SetSuccessor(currentBlockBuilder.Index + 1);
                    }

                    // The line is of form "BB_nnn:" so we have to chop bits off both ends
                    var blockIndex = int.Parse(currentLine.Substring(3, currentLine.Length - 4));
                    
                    // Blocks may be omitted in the disassembly, so we may need to create the missing ones too
                    while (currentBlockBuilder == null || currentBlockBuilder.Index < blockIndex)
                    {
                        currentBlockBuilder = graphBuilder.GetNewBasicBlock();
                    }
                    Assert.That(blockIndex, Is.EqualTo(currentBlockBuilder.Index), "Blocks must be specified in order.");
                }
                else if (currentLine.StartsWith("==>"))
                {
                    // Explicitly set the successor block
                    // The line is of form "==> BB_nnn"
                    var blockIndex = int.Parse(currentLine.Substring(7));

                    Assert.That(currentBlockBuilder, Is.Not.Null);
                    currentBlockBuilder.SetSuccessor(blockIndex);
                }
                else
                {
                    Assert.That(currentBlockBuilder, Is.Not.Null, "No basic block has been started.");

                    ParseInstruction(currentLine, method, currentBlockBuilder);
                }
            }

            method.Body = graphBuilder.Build();
            return method;
        }

        private static void ParseLocal(string line, CompiledMethod method)
        {
            var lineParts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            // Get the local index by removing the preceding #, and validate it
            var localIndex = int.Parse(lineParts[1].Substring(1));
            Assert.That(localIndex, Is.EqualTo(method.Values.Count), "Local indices must be specified in order.");

            // Get the type and initial value
            var type = ResolveType(lineParts[2]);
            var isParam = lineParts.Length >= 4 && lineParts[3] == "param";

            method.AddLocal(type, isParam ? LocalFlags.Parameter : LocalFlags.None);
        }

        private static void ParseInstruction(string line, CompiledMethod method, BasicBlockBuilder builder)
        {
            var lineParts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            Assert.That(Enum.TryParse<Opcode>(lineParts[0], out var opcode), Is.True, $"Unknown opcode: {lineParts[0]}");

            // TODO: The remaining opcodes
            if (opcode == Opcode.Return)
            {
                // Remove leading # before parsing the value number
                var sourceIndex = ushort.Parse(lineParts[1].Substring(1));

                builder.AppendInstruction(Opcode.Return, sourceIndex, 0, 0);
            }
            else if (opcode == Opcode.BranchIf)
            {
                var sourceIndex = ushort.Parse(lineParts[1].Substring(1));
                var targetBlockIndex = int.Parse(lineParts[3].Substring(3));

                builder.AppendInstruction(Opcode.BranchIf, sourceIndex, 0, 0);
                builder.SetAlternativeSuccessor(targetBlockIndex);
            }
            else if (opcode == Opcode.Load)
            {
                var value = ResolveValue(lineParts[1]);
                var destIndex = ushort.Parse(lineParts[3].Substring(1));
                
                builder.AppendInstruction(Opcode.Load, value, 0, destIndex);
            }
            else if (IsUnary(opcode))
            {
                var sourceIndex = ushort.Parse(lineParts[1].Substring(1));
                var destIndex = ushort.Parse(lineParts[3].Substring(1));

                builder.AppendInstruction(opcode, sourceIndex, 0, destIndex);
            }
            else if (IsBinary(opcode))
            {
                var leftIndex = ushort.Parse(lineParts[1].Substring(1));
                var rightIndex = ushort.Parse(lineParts[3].Substring(1));
                var destIndex = ushort.Parse(lineParts[5].Substring(1));

                builder.AppendInstruction(opcode, leftIndex, rightIndex, destIndex);
            }
            else
            {
                throw new NotImplementedException($"Unimplemented opcode to assemble: {opcode}");
            }
        }

        private static TypeDefinition ResolveType(string typeName)
        {
            switch (typeName)
            {
                case "bool":
                    return SimpleType.Bool;
                case "int32":
                    return SimpleType.Int32;
                case "void":
                    return SimpleType.Void;
                default:
                    throw new NotImplementedException($"Unimplemented type to assemble: {typeName}");
            }
        }

        private static ulong ResolveValue(string valueString)
        {
            if (valueString == "false")
            {
                return 0;
            }
            else if (valueString == "true")
            {
                return 1;
            }
            else if (int.TryParse(valueString, out var intValue))
            {
                return (ulong)intValue;
            }
            else
            {
                throw new NotImplementedException($"Unimplemented value to assemble: {valueString}");
            }
        }

        private static bool IsUnary(Opcode opcode)
        {
            switch (opcode)
            {
                case Opcode.ArithmeticNegate:
                case Opcode.BitwiseNot:
                case Opcode.CopyValue:
                    return true;
                default:
                    return false;
            }
        }

        private static bool IsBinary(Opcode opcode)
        {
            switch (opcode)
            {
                case Opcode.Add:
                case Opcode.Subtract:
                case Opcode.Multiply:
                case Opcode.Divide:
                case Opcode.Modulo:
                case Opcode.BitwiseAnd:
                case Opcode.BitwiseOr:
                case Opcode.BitwiseXor:
                case Opcode.ShiftLeft:
                case Opcode.ShiftRight:
                case Opcode.Less:
                case Opcode.LessOrEqual:
                case Opcode.Equal:
                    return true;
                default:
                    return false;
            }
        }
    }
}

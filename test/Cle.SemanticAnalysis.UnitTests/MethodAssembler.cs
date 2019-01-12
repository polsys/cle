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
                    // The line is of form "BB_nnn:" so we have to chop bits off both ends
                    var blockIndex = int.Parse(currentLine.Substring(3, currentLine.Length - 4));
                    
                    // Blocks may be omitted in the disassembly, so we may need to create the missing ones too
                    while (currentBlockBuilder == null || currentBlockBuilder.Index < blockIndex)
                    {
                        currentBlockBuilder = graphBuilder.GetNewBasicBlock();
                    }
                    Assert.That(blockIndex, Is.EqualTo(currentBlockBuilder.Index), "Blocks must be specified in order.");
                }
                else
                {
                    Assert.That(currentBlockBuilder, Is.Not.Null, "No basic block has been started.");

                    ParseInstruction(currentLine, method, currentBlockBuilder);
                }

                // TODO: Default successor behavior (fallthrough, explicit)
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
            var value = ResolveValue(lineParts[4]);

            method.AddLocal(type, value);
        }

        private static void ParseInstruction(string line, CompiledMethod method, BasicBlockBuilder builder)
        {
            var lineParts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            Assert.That(Enum.TryParse<Opcode>(lineParts[0], out var opcode), Is.True, $"Unknown opcode: {lineParts[0]}");

            // TODO: The remaining opcodes
            if (opcode == Opcode.Return)
            {
                // Remove leading # before parsing the value number
                var sourceIndex = int.Parse(lineParts[1].Substring(1));

                builder.AppendInstruction(Opcode.Return, sourceIndex, 0, 0);
            }
            else if (IsUnary(opcode))
            {
                var sourceIndex = int.Parse(lineParts[1].Substring(1));
                var destIndex = int.Parse(lineParts[3].Substring(1));

                builder.AppendInstruction(opcode, sourceIndex, 0, destIndex);
            }
            else if (IsBinary(opcode))
            {
                var leftIndex = int.Parse(lineParts[1].Substring(1));
                var rightIndex = int.Parse(lineParts[3].Substring(1));
                var destIndex = int.Parse(lineParts[5].Substring(1));

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

        private static ConstantValue ResolveValue(string valueString)
        {
            if (valueString == "void")
            {
                return ConstantValue.Void();
            }
            else if (valueString == "param")
            {
                return ConstantValue.Parameter();
            }
            else if (valueString == "false")
            {
                return ConstantValue.Bool(false);
            }
            else if (valueString == "true")
            {
                return ConstantValue.Bool(true);
            }
            else if (int.TryParse(valueString, out var intValue))
            {
                return ConstantValue.SignedInteger(intValue);
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

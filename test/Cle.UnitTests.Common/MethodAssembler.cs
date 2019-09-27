using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Cle.Common.TypeSystem;
using Cle.SemanticAnalysis;
using Cle.SemanticAnalysis.IR;
using NUnit.Framework;

namespace Cle.UnitTests.Common
{
    /// <summary>
    /// An UNRELIABLE intermediate representation assembler for TESTING ONLY.
    /// </summary>
    public static class MethodAssembler
    {
        /// <summary>
        /// Creates a <see cref="CompiledMethod"/> out of the given IR disassembly,
        /// which must follow the <see cref="MethodDisassembler"/> output very closely.
        /// </summary>
        public static CompiledMethod Assemble(string source, string fullName)
        {
            var method = new CompiledMethod(fullName);
            var graphBuilder = new BasicBlockGraphBuilder();
            BasicBlockBuilder? currentBlockBuilder = null;
            var calledMethodIndices = new Dictionary<string, int>();
            
            // Since this class is for testing purposes only, we use brittle and unperformant string splits
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
                    var blockIndex = int.Parse(currentLine.AsSpan(3, currentLine.Length - 4));
                    
                    currentBlockBuilder = graphBuilder.GetNewBasicBlock();
                    Assert.That(blockIndex, Is.EqualTo(currentBlockBuilder.Index), "Blocks must be specified in order.");
                }
                else if (currentLine.StartsWith("==>"))
                {
                    // Explicitly set the successor block
                    // The line is of form "==> BB_nnn"
                    var blockIndex = int.Parse(currentLine.AsSpan(7));

                    Assert.That(currentBlockBuilder, Is.Not.Null, "No basic block has been started.");
                    currentBlockBuilder!.SetSuccessor(blockIndex);
                }
                else if (currentLine.StartsWith("PHI"))
                {
                    Assert.That(currentBlockBuilder, Is.Not.Null, "No basic block has been started.");

                    // Phi: read the unknown number of operands - the last one is the destination
                    var phiParts = currentLine.Split(' ', '(', ',', ')');
                    var phiBuilder = ImmutableList<int>.Empty.ToBuilder();

                    foreach (var operand in phiParts)
                    {
                        if (operand.StartsWith("#"))
                        {
                            phiBuilder.Add(int.Parse(operand.AsSpan(1)));
                        }
                    }
                    var dest = phiBuilder[phiBuilder.Count - 1];
                    phiBuilder.RemoveAt(phiBuilder.Count - 1);
                    
                    currentBlockBuilder!.AddPhi(dest, phiBuilder.ToImmutable());
                }
                else
                {
                    Assert.That(currentBlockBuilder, Is.Not.Null, "No basic block has been started.");

                    ParseInstruction(currentLine, method, currentBlockBuilder!, calledMethodIndices);
                }
            }

            method.Body = graphBuilder.Build();
            return method;
        }

        private static void ParseLocal(string line, CompiledMethod method)
        {
            var lineParts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            // Get the local index by removing the preceding #, and validate it
            var localIndex = int.Parse(lineParts[1].AsSpan(1));
            Assert.That(localIndex, Is.EqualTo(method.Values.Count), "Local indices must be specified in order.");

            // Get the type and initial value
            var type = ResolveType(lineParts[2]);
            var isParam = lineParts.Length >= 4 && lineParts[3] == "param";

            method.AddLocal(type, isParam ? LocalFlags.Parameter : LocalFlags.None);
        }

        private static void ParseInstruction(string line, CompiledMethod method, BasicBlockBuilder builder,
            Dictionary<string, int> calledMethodIndices)
        {
            var lineParts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            Assert.That(Enum.TryParse<Opcode>(lineParts[0], out var opcode), Is.True, $"Unknown opcode: {lineParts[0]}");
            
            if (opcode == Opcode.Return)
            {
                // Remove leading # before parsing the value number
                var sourceIndex = ushort.Parse(lineParts[1].AsSpan(1));

                builder.AppendInstruction(Opcode.Return, sourceIndex, 0, 0);
            }
            else if (opcode == Opcode.BranchIf)
            {
                var sourceIndex = ushort.Parse(lineParts[1].AsSpan(1));
                var targetBlockIndex = int.Parse(lineParts[3].AsSpan(3));

                builder.AppendInstruction(Opcode.BranchIf, sourceIndex, 0, 0);
                builder.SetAlternativeSuccessor(targetBlockIndex);
            }
            else if (opcode == Opcode.Load)
            {
                var value = ResolveValue(lineParts[1]);
                var destIndex = ushort.Parse(lineParts[3].AsSpan(1));
                
                builder.AppendInstruction(Opcode.Load, value, 0, destIndex);
            }
            else if (opcode == Opcode.Call)
            {
                // Emulate function indices by creating them sequentially upwards from 100
                // If a function is called twice, it will have the same index both times
                var functionName = lineParts[1].Substring(0, lineParts[1].IndexOf('('));
                if (!calledMethodIndices.TryGetValue(functionName, out var functionIndex))
                {
                    functionIndex = 100 + calledMethodIndices.Count;
                    calledMethodIndices.Add(functionName, functionIndex);
                }

                var destIndex = ushort.Parse(lineParts[^1].AsSpan(1));
                var paramListStart = line.IndexOf('(') + 1;
                var parameterList = line.Substring(paramListStart, line.IndexOf(')') - paramListStart);

                var paramLocals = new List<int>();
                foreach (var param in parameterList.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    paramLocals.Add(int.Parse(param.AsSpan(1)));
                }

                // The call may include a suffix before the "->" - try to parse it
                var callType = lineParts[^3] == "import" ?
                    MethodCallType.Imported :
                    MethodCallType.Native;

                builder.AppendInstruction(Opcode.Call,
                    method.AddCallInfo(functionIndex, paramLocals.ToArray(), functionName, callType), 0, destIndex);
            }
            else if (IsUnary(opcode))
            {
                var sourceIndex = ushort.Parse(lineParts[1].AsSpan(1));
                var destIndex = ushort.Parse(lineParts[3].AsSpan(1));

                builder.AppendInstruction(opcode, sourceIndex, 0, destIndex);
            }
            else if (IsBinary(opcode))
            {
                var leftIndex = ushort.Parse(lineParts[1].AsSpan(1));
                var rightIndex = ushort.Parse(lineParts[3].AsSpan(1));
                var destIndex = ushort.Parse(lineParts[5].AsSpan(1));

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

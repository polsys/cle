using System;
using System.Collections.Generic;
using System.IO;
using Cle.CodeGeneration.Lir;
using Cle.SemanticAnalysis.IR;
using JetBrains.Annotations;

namespace Cle.CodeGeneration
{
    public sealed class WindowsX64CodeGenerator
    {
        [NotNull] private readonly PortableExecutableWriter _peWriter;
        [NotNull] private readonly List<Fixup> _fixupsForMethod;
        [NotNull] private readonly List<int> _blockPositions;

        /// <summary>
        /// Creates a code generator instance with the specified output stream and optional disassembly stream.
        /// </summary>
        /// <param name="outputStream">
        /// Stream for the native code output.
        /// The stream must be writable, seekable and initially empty.
        /// The object lifetime is managed by the caller.
        /// </param>
        /// <param name="disassemblyWriter">
        /// Optional text writer for method disassembly.
        /// </param>
        public WindowsX64CodeGenerator([NotNull] Stream outputStream, [CanBeNull] TextWriter disassemblyWriter)
        {
            _peWriter = new PortableExecutableWriter(outputStream, disassemblyWriter);
            _fixupsForMethod = new List<Fixup>();
            _blockPositions = new List<int>();
        }

        /// <summary>
        /// Completes the executable file.
        /// </summary>
        public void FinalizeFile()
        {
            _peWriter.FinalizeFile();
        }

        /// <summary>
        /// Emits native code for the given method.
        /// </summary>
        /// <param name="method">A compiled method in SSA form.</param>
        /// <param name="methodIndex">The compiler internal index for the method.</param>
        /// <param name="isEntryPoint">If true, this method is marked as the executable entry point.</param>
        public void EmitMethod([NotNull] CompiledMethod method, int methodIndex, bool isEntryPoint)
        {
            _fixupsForMethod.Clear();
            _blockPositions.Clear();

            _peWriter.StartNewMethod(methodIndex, method.FullName);
            if (isEntryPoint)
            {
                // TODO: Emit a compiler-generated entry point that calls the user-defined entry point
                _peWriter.MarkEntryPoint();
            }

            // Lower the IR to a low-level form
            var loweredMethod = LoweringX64.Lower(method);

            // TODO: Debug log the lowering
            // TODO: Does the LIR need some other optimization (block merging, etc.) before peephole?
            // Perform peephole optimization
            PeepholeOptimizer<X64Register>.Optimize(loweredMethod);

            // Allocate registers for locals (with special casing for parameters)
            X64RegisterAllocator.Allocate(loweredMethod);

            // Emit the lowered IR
            for (var i = 0; i < loweredMethod.Blocks.Count; i++)
            {
                _peWriter.Emitter.StartBlock(i, out var position);
                _blockPositions.Add(position);
                EmitBlock(i, loweredMethod);
            }

            // Apply fixups
            foreach (var fixup in _fixupsForMethod)
            {
                _peWriter.Emitter.ApplyFixup(fixup, _blockPositions[fixup.Tag]);
            }
        }

        private void EmitBlock(int blockIndex, LowMethod<X64Register> method)
        {
            var block = method.Blocks[blockIndex];
            var emitter = _peWriter.Emitter;

            foreach (var inst in block.Instructions)
            {
                switch (inst.Op)
                {
                    case LowOp.LoadInt:
                        emitter.EmitLoad(method.Locals[inst.Dest].Location, inst.Data);
                        break;
                    case LowOp.Move:
                    {
                        var sourceLocation = method.Locals[inst.Left].Location;
                        var destLocation = method.Locals[inst.Dest].Location;
                        var operandSize = method.Locals[inst.Dest].Type.SizeInBytes;

                        if (sourceLocation != destLocation)
                        {
                            emitter.EmitMov(destLocation, sourceLocation, operandSize);
                        }
                        break;
                    }
                    case LowOp.IntegerAdd:
                        EmitIntegerBinaryOp(BinaryOp.Add, in inst, method);
                        break;
                    case LowOp.Compare:
                        emitter.EmitCmp(method.Locals[inst.Left].Location, method.Locals[inst.Right].Location);
                        break;
                    case LowOp.Test:
                    {
                        var srcDestLocation = method.Locals[inst.Left].Location;
                        emitter.EmitTest(srcDestLocation, srcDestLocation);
                        break;
                    }
                    case LowOp.SetIfEqual:
                        EmitConditionalSet(X64Condition.Equal, method.Locals[inst.Dest].Location);
                        break;
                    case LowOp.SetIfNotEqual:
                        EmitConditionalSet(X64Condition.NotEqual, method.Locals[inst.Dest].Location);
                        break;
                    case LowOp.SetIfLess:
                        EmitConditionalSet(X64Condition.Less, method.Locals[inst.Dest].Location);
                        break;
                    case LowOp.SetIfLessOrEqual:
                        EmitConditionalSet(X64Condition.LessOrEqual, method.Locals[inst.Dest].Location);
                        break;
                    case LowOp.SetIfGreater:
                        EmitConditionalSet(X64Condition.Greater, method.Locals[inst.Dest].Location);
                        break;
                    case LowOp.SetIfGreaterOrEqual:
                        EmitConditionalSet(X64Condition.GreaterOrEqual, method.Locals[inst.Dest].Location);
                        break;
                    case LowOp.Jump:
                    {
                        // Do not emit a jump for a simple fallthrough
                        if (inst.Dest == blockIndex + 1)
                            return;

                        // Don't bother creating a fixup for a backward branch where the destination is already known
                        if (inst.Dest <= blockIndex)
                        {
                            emitter.EmitJmp(inst.Dest, _blockPositions[inst.Dest]);
                        }
                        else
                        {
                            emitter.EmitJmpWithFixup(inst.Dest, out var fixup);
                            _fixupsForMethod.Add(fixup);
                        }
                        break;
                    }
                    case LowOp.JumpIfEqual:
                        EmitConditionalJump(X64Condition.Equal, inst.Dest, blockIndex);
                        break;
                    case LowOp.JumpIfNotEqual:
                        EmitConditionalJump(X64Condition.NotEqual, inst.Dest, blockIndex);
                        break;
                    case LowOp.JumpIfLess:
                        EmitConditionalJump(X64Condition.Less, inst.Dest, blockIndex);
                        break;
                    case LowOp.JumpIfLessOrEqual:
                        EmitConditionalJump(X64Condition.LessOrEqual, inst.Dest, blockIndex);
                        break;
                    case LowOp.JumpIfGreater:
                        EmitConditionalJump(X64Condition.Greater, inst.Dest, blockIndex);
                        break;
                    case LowOp.JumpIfGreaterOrEqual:
                        EmitConditionalJump(X64Condition.GreaterOrEqual, inst.Dest, blockIndex);
                        break;
                    case LowOp.Return:
                        emitter.EmitRet();
                        return;
                    case LowOp.Nop:
                        break;
                    default:
                        throw new NotImplementedException("Unimplemented LIR opcode: " + inst.Op);
                }
            }
        }

        private void EmitConditionalJump(X64Condition condition, int targetBlock, int currentBlock)
        {
            var emitter = _peWriter.Emitter;

            if (targetBlock <= currentBlock)
            {
                emitter.EmitJcc(condition, targetBlock, _blockPositions[targetBlock]);
            }
            else
            {
                emitter.EmitJccWithFixup(condition, targetBlock, out var fixup);
                _fixupsForMethod.Add(fixup);
            }
        }

        private void EmitConditionalSet(X64Condition condition, in StorageLocation<X64Register> destLocation)
        {
            var emitter = _peWriter.Emitter;

            // We must zero-extend manually since setcc sets only the low 8 bits
            emitter.EmitSetcc(condition, destLocation);
            emitter.EmitZeroExtendFromByte(destLocation);
        }

        private void EmitIntegerBinaryOp(BinaryOp op, in LowInstruction inst, LowMethod<X64Register> method)
        {
            var emitter = _peWriter.Emitter;

            var leftLocation = method.Locals[inst.Left].Location;
            var rightLocation = method.Locals[inst.Right].Location;
            var destLocation = method.Locals[inst.Dest].Location;
            var operandSize = method.Locals[inst.Dest].Type.SizeInBytes;

            if (leftLocation == destLocation)
            {
                // The ideal case: we can emit "op left, right"
                emitter.EmitGeneralBinaryOp(op, leftLocation, rightLocation, operandSize);
            }
            else if (rightLocation == destLocation)
            {
                // Still good: "op right, left"
                emitter.EmitGeneralBinaryOp(op, rightLocation, leftLocation, operandSize);
            }
            else
            {
                // We have to do a temporary move first
                emitter.EmitMov(destLocation, leftLocation, operandSize);
                emitter.EmitGeneralBinaryOp(op, destLocation, rightLocation, operandSize);
            }
        }
    }
}

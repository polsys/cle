﻿using System;
using System.Diagnostics;
using Cle.CodeGeneration.Lir;
using Cle.Common.TypeSystem;
using Cle.SemanticAnalysis.IR;
using JetBrains.Annotations;

namespace Cle.CodeGeneration
{
    /// <summary>
    /// This class implements the lowering of IR to LIR.
    /// </summary>
    internal static class LoweringX64
    {
        public static LowMethod<X64Register> Lower([NotNull] CompiledMethod highMethod)
        {
            Debug.Assert(highMethod.Body != null);
            Debug.Assert(highMethod.Body.BasicBlocks.Count > 0);

            var lowMethod = new LowMethod<X64Register>();

            // Create locals for SSA values
            // Additional locals may be created by instructions
            var paramCount = 0;
            foreach (var value in highMethod.Values)
            {
                if (value.Flags.HasFlag(LocalFlags.Parameter))
                {
                    paramCount++;
                }
                lowMethod.Locals.Add(new LowLocal<X64Register>(value.Type));
            }

            // Convert each basic block
            for (var i = 0; i < highMethod.Body.BasicBlocks.Count; i++)
            {
                var highBlock = highMethod.Body.BasicBlocks[i];

                if (highBlock is null)
                {
                    lowMethod.Blocks.Add(new LowBlock()
                    {
                        Predecessors = Array.Empty<int>(),
                        Successors = Array.Empty<int>()
                    });
                }
                else
                {
                    lowMethod.Blocks.Add(ConvertBlock(highBlock, highMethod, lowMethod, i == 0, paramCount));
                }
            }

            return lowMethod;
        }

        private static StorageLocation<X64Register> GetLocationForParameter(int paramIndex)
        {
            switch (paramIndex)
            {
                case 0:
                    return new StorageLocation<X64Register>(X64Register.Rcx);
                case 1:
                    return new StorageLocation<X64Register>(X64Register.Rdx);
                case 2:
                    return new StorageLocation<X64Register>(X64Register.R8);
                case 3:
                    return new StorageLocation<X64Register>(X64Register.R9);
                default:
                    throw new NotImplementedException("Parameters on stack");
            }
        }

        private static LowBlock ConvertBlock([NotNull] BasicBlock highBlock, [NotNull] CompiledMethod highMethod,
            [NotNull] LowMethod<X64Register> methodInProgress, bool isFirstBlock, int paramCount)
        {
            var lowBlock = new LowBlock
            {
                Phis = highBlock.Phis,
                Predecessors = highBlock.Predecessors
            };

            // Initialize the list of successors
            if (highBlock.AlternativeSuccessor >= 0)
            {
                lowBlock.Successors = new[] { highBlock.AlternativeSuccessor, highBlock.DefaultSuccessor };
            }
            else if (highBlock.DefaultSuccessor >= 0)
            {
                lowBlock.Successors = new[] { highBlock.DefaultSuccessor };
            }
            else
            {
                lowBlock.Successors = Array.Empty<int>();
            }

            // At the start of the first block, we must copy parameters from fixed-location temps to freely assigned locals
            if (isFirstBlock)
            {
                // This assumes that the first paramCount locals are the parameters
                for (var i = 0; i < paramCount; i++)
                {
                    methodInProgress.Locals.Add(
                        new LowLocal<X64Register>(highMethod.Values[i].Type, GetLocationForParameter(i)));
                    var tempIndex = methodInProgress.Locals.Count - 1;

                    lowBlock.Instructions.Add(new LowInstruction(LowOp.Move, i, tempIndex, 0, 0));
                }
            }

            // Convert the instructions
            var returns = false;
            foreach (var inst in highBlock.Instructions)
            {
                switch (inst.Operation)
                {
                    case Opcode.Add:
                    case Opcode.BitwiseAnd:
                    case Opcode.BitwiseOr:
                    case Opcode.BitwiseXor:
                    case Opcode.Multiply:
                    case Opcode.Subtract:
                        ConvertBinaryArithmetic(inst.Operation, in inst, lowBlock, methodInProgress);
                        break;
                    case Opcode.ArithmeticNegate:
                        ConvertUnaryArithmetic(Opcode.ArithmeticNegate, in inst, lowBlock, methodInProgress);
                        break;
                    case Opcode.BitwiseNot:
                        if (methodInProgress.Locals[(int)inst.Left].Type.Equals(SimpleType.Bool))
                        {
                            // For booleans, BitwiseNot is interpreted as a logical NOT.
                            // Convert it into a Test followed by SetIfZero (SetIfEqual)
                            lowBlock.Instructions.Add(new LowInstruction(LowOp.Test, 0, (int)inst.Left, 0, 0));
                            lowBlock.Instructions.Add(new LowInstruction(LowOp.SetIfEqual, inst.Destination, 0, 0, 0));
                        }
                        else
                        {
                            ConvertUnaryArithmetic(Opcode.BitwiseNot, in inst, lowBlock, methodInProgress);
                        }
                        break;
                    case Opcode.BranchIf:
                        ConvertBranchIf(lowBlock, highBlock, (int)inst.Left);
                        break;
                    case Opcode.Call:
                        ConvertCall(lowBlock, highMethod.CallInfos[(int)inst.Left], (int)inst.Left, inst.Destination,
                            methodInProgress);
                        break;
                    case Opcode.Divide:
                        ConvertDivisionOrModulo(Opcode.Divide, in inst, lowBlock, methodInProgress);
                        break;
                    case Opcode.Equal:
                        ConvertCompare(in inst, LowOp.SetIfEqual, lowBlock);
                        break;
                    case Opcode.Less:
                        ConvertCompare(in inst, LowOp.SetIfLess, lowBlock);
                        break;
                    case Opcode.LessOrEqual:
                        ConvertCompare(in inst, LowOp.SetIfLessOrEqual, lowBlock);
                        break;
                    case Opcode.Load:
                        lowBlock.Instructions.Add(new LowInstruction(LowOp.LoadInt, inst.Destination, 0, 0, inst.Left));
                        break;
                    case Opcode.Modulo:
                        ConvertDivisionOrModulo(Opcode.Modulo, in inst, lowBlock, methodInProgress);
                        break;
                    case Opcode.Return:
                        returns = true;
                        ConvertReturn(lowBlock, (int)inst.Left, highMethod, methodInProgress);
                        break;
                    default:
                        throw new NotImplementedException("Unimplemented opcode to lower: " + inst.Operation);
                }
            }

            if (!returns)
            {
                lowBlock.Instructions.Add(new LowInstruction(LowOp.Jump, highBlock.DefaultSuccessor, 0, 0, 0));
            }

            return lowBlock;
        }

        private static void ConvertBinaryArithmetic(Opcode op, in Instruction inst, LowBlock lowBlock,
            LowMethod<X64Register> methodInProgress)
        {
            if (methodInProgress.Locals[(int)inst.Left].Type.Equals(SimpleType.Int32) &&
                methodInProgress.Locals[inst.Right].Type.Equals(SimpleType.Int32))
            {
                var lowOp = GetLoweredIntegerArithmeticOp(op);
                lowBlock.Instructions.Add(new LowInstruction(lowOp, inst.Destination, (int)inst.Left, inst.Right, 0));
            }
            else
            {
                throw new NotImplementedException("Floating-point arithmetic: " + op);
            }
        }

        private static void ConvertUnaryArithmetic(Opcode op, in Instruction inst, LowBlock lowBlock,
            LowMethod<X64Register> methodInProgress)
        {
            if (methodInProgress.Locals[(int)inst.Left].Type.Equals(SimpleType.Int32))
            {
                var lowOp = GetLoweredIntegerArithmeticOp(op);
                lowBlock.Instructions.Add(new LowInstruction(lowOp, inst.Destination, (int)inst.Left, 0, 0));
            }
            else
            {
                throw new NotImplementedException("Floating-point arithmetic: " + op);
            }
        }

        private static void ConvertDivisionOrModulo(Opcode op, in Instruction inst, LowBlock lowBlock,
            LowMethod<X64Register> methodInProgress)
        {
            if (!methodInProgress.Locals[(int)inst.Left].Type.Equals(SimpleType.Int32) &&
                !methodInProgress.Locals[inst.Right].Type.Equals(SimpleType.Int32))
            {
                throw new NotImplementedException("Non-int32 arithmetic: " + op);
            }

            // On x64, the dividend is stored in edx:eax and after the operation, the result in eax
            // and the remainder in edx. Therefore we must emit fixed-location temporaries.

            var dividendIndex = methodInProgress.Locals.Count;
            methodInProgress.Locals.Add(new LowLocal<X64Register>(SimpleType.Int32, X64Register.Rax));
            lowBlock.Instructions.Add(new LowInstruction(LowOp.Move, dividendIndex, (int)inst.Left, 0, 0));

            var resultIndex = methodInProgress.Locals.Count;

            if (op == Opcode.Divide)
            {
                methodInProgress.Locals.Add(new LowLocal<X64Register>(SimpleType.Int32, X64Register.Rax));

                lowBlock.Instructions.Add(new LowInstruction(
                    LowOp.IntegerDivide, resultIndex, dividendIndex, inst.Right, 0));
                lowBlock.Instructions.Add(new LowInstruction(LowOp.Move, inst.Destination, resultIndex, 0, 0));
            }
            else if (op == Opcode.Modulo)
            {
                methodInProgress.Locals.Add(new LowLocal<X64Register>(SimpleType.Int32, X64Register.Rdx));

                lowBlock.Instructions.Add(new LowInstruction(
                    LowOp.IntegerModulo, resultIndex, dividendIndex, inst.Right, 0));
                lowBlock.Instructions.Add(new LowInstruction(LowOp.Move, inst.Destination, resultIndex, 0, 0));
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(op));
            }
        }

        private static void ConvertBranchIf(LowBlock lowBlock, BasicBlock highBlock, int valueIndex)
        {
            // In high IR an if (a == b) branch is represented as
            //   Equal a == b -> c
            //   BranchIf c ==> dest.
            // We lower this to
            //   Compare a, b
            //   SetEqual c
            //   Test c
            //   JumpIfNotEqual dest. (note: this tests that c != 0)
            // This is suboptimal, but enables sharing lowering code with if (c) branches.
            // A peephole pattern transforms the LIR to
            //   Compare a, b
            //   JumpIfEqual dest.
            lowBlock.Instructions.Add(new LowInstruction(LowOp.Test, 0, valueIndex, 0, 0));
            lowBlock.Instructions.Add(new LowInstruction(LowOp.JumpIfNotEqual, highBlock.AlternativeSuccessor, 0, 0, 0));
        }

        private static void ConvertCompare(in Instruction inst, LowOp op, LowBlock lowBlock)
        {
            lowBlock.Instructions.Add(new LowInstruction(LowOp.Compare, 0, (int)inst.Left, inst.Right, 0));
            lowBlock.Instructions.Add(new LowInstruction(op, inst.Destination, 0, 0, 0));
        }

        private static void ConvertReturn(LowBlock lowBlock, int valueIndex, CompiledMethod highMethod,
            LowMethod<X64Register> methodInProgress)
        {
            // Convert
            //   Return x
            // into
            //   Move x -> return_reg
            //   Return,
            // unless this is a void method, in which case the return value is zeroed.
            // (The zeroing is needed because the register allocator expects uses to be preceded
            // by definitions.)
            // TODO: Replace the zeroing with a dummy use that leaves the value undefined
            var returnValue = highMethod.Values[valueIndex];

            var dest = methodInProgress.Locals.Count;
            methodInProgress.Locals.Add(new LowLocal<X64Register>(returnValue.Type, X64Register.Rax));

            if (returnValue.Type.Equals(SimpleType.Void))
            {
                lowBlock.Instructions.Add(new LowInstruction(LowOp.LoadInt, dest, 0, 0, 0));
            }
            else
            {
                lowBlock.Instructions.Add(new LowInstruction(LowOp.Move, dest, valueIndex, 0, 0));
            }

            lowBlock.Instructions.Add(new LowInstruction(LowOp.Return, 0, dest, 0, 0));
        }

        private static void ConvertCall(LowBlock lowBlock, MethodCallInfo callInfo, int callInfoIndex, ushort dest,
            LowMethod<X64Register> methodInProgress)
        {
            // Move parameters to correct locations
            // These are represented by short-lived temporaries in fixed locations,
            // and it is up to the register allocator to optimize this
            for (var i = 0; i < callInfo.ParameterIndices.Length; i++)
            {
                var paramIndex = callInfo.ParameterIndices[i];
                methodInProgress.Locals.Add(new LowLocal<X64Register>(methodInProgress.Locals[paramIndex].Type,
                   GetLocationForParameter(i)));

                lowBlock.Instructions.Add(new LowInstruction(LowOp.Move, methodInProgress.Locals.Count - 1, paramIndex, 0, 0));
            }

            methodInProgress.Locals.Add(new LowLocal<X64Register>(methodInProgress.Locals[dest].Type, X64Register.Rax));
            var destLocal = methodInProgress.Locals.Count - 1;
            lowBlock.Instructions.Add(new LowInstruction(LowOp.Call, destLocal,
                callInfoIndex, 0, (uint)callInfo.CalleeIndex));

            // Then, unless the method returns void, do the same for the return value
            if (!methodInProgress.Locals[dest].Type.Equals(SimpleType.Void))
            {
                lowBlock.Instructions.Add(new LowInstruction(LowOp.Move, dest, destLocal, 0, 0));
            }
        }

        private static LowOp GetLoweredIntegerArithmeticOp(Opcode op)
        {
            switch (op)
            {
                case Opcode.Add:
                    return LowOp.IntegerAdd;
                case Opcode.ArithmeticNegate:
                    return LowOp.IntegerNegate;
                case Opcode.BitwiseAnd:
                    return LowOp.BitwiseAnd;
                case Opcode.BitwiseNot:
                    return LowOp.BitwiseNot;
                case Opcode.BitwiseOr:
                    return LowOp.BitwiseOr;
                case Opcode.BitwiseXor:
                    return LowOp.BitwiseXor;
                case Opcode.Subtract:
                    return LowOp.IntegerSubtract;
                case Opcode.Multiply:
                    return LowOp.IntegerMultiply;
                default:
                    throw new ArgumentOutOfRangeException(nameof(op));
            }
        }
    }
}

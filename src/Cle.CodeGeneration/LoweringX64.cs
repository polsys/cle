using System;
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
                    lowMethod.Locals.Add(new LowLocal<X64Register>(value.Type)
                    {
                        Location = GetLocationForParameter(paramCount)
                    });
                    paramCount++;
                }
                else
                {
                    lowMethod.Locals.Add(new LowLocal<X64Register>(value.Type));
                }
            }

            // Convert each basic block
            foreach (var highBlock in highMethod.Body.BasicBlocks)
            {
                if (highBlock is null)
                {
                    lowMethod.Blocks.Add(new LowBlock());
                }
                else
                {
                    lowMethod.Blocks.Add(ConvertBlock(highBlock, highMethod, lowMethod));
                }
            }

            // TODO: Convert PHIs into copies

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
            [NotNull] LowMethod<X64Register> methodInProgress)
        {
            var lowBlock = new LowBlock
            {
                Phis = highBlock.Phis
            };

            // Convert the instructions
            var returns = false;
            foreach (var inst in highBlock.Instructions)
            {
                switch (inst.Operation)
                {
                    case Opcode.Add:
                        if (methodInProgress.Locals[(int)inst.Left].Type.Equals(SimpleType.Int32) &&
                            methodInProgress.Locals[inst.Right].Type.Equals(SimpleType.Int32))
                        {
                            lowBlock.Instructions.Add(
                                new LowInstruction(LowOp.IntegerAdd, inst.Destination, (int)inst.Left, inst.Right, 0));
                        }
                        else
                        {
                            goto default;
                        }
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
                            goto default;
                        }
                        break;
                    case Opcode.BranchIf:
                        ConvertBranchIf(lowBlock, highBlock, (int)inst.Left);
                        break;
                    case Opcode.Call:
                        ConvertCall(lowBlock, highMethod.CallInfos[(int)inst.Left], (int)inst.Left, inst.Destination,
                            methodInProgress);
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
            // unless this is a void method (in which case the return value is undefined).
            var returnValue = highMethod.Values[valueIndex];

            if (!returnValue.Type.Equals(SimpleType.Void))
            {
                var dest = methodInProgress.Locals.Count;
                methodInProgress.Locals.Add(new LowLocal<X64Register>(returnValue.Type)
                    { Location = new StorageLocation<X64Register>(X64Register.Rax) });

                lowBlock.Instructions.Add(new LowInstruction(LowOp.Move, dest, valueIndex, 0, 0));
            }

            lowBlock.Instructions.Add(new LowInstruction(LowOp.Return, 0, 0, 0, 0));
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
                methodInProgress.Locals.Add(new LowLocal<X64Register>(methodInProgress.Locals[paramIndex].Type)
                {
                    Location = GetLocationForParameter(i)
                });

                lowBlock.Instructions.Add(new LowInstruction(LowOp.Move, methodInProgress.Locals.Count - 1, paramIndex, 0, 0));
            }

            lowBlock.Instructions.Add(new LowInstruction(LowOp.Call, 0, callInfoIndex, 0, (uint)callInfo.CalleeIndex));

            // Then, unless the method returns void, do the same for the return value
            if (!methodInProgress.Locals[dest].Type.Equals(SimpleType.Void))
            {
                methodInProgress.Locals.Add(new LowLocal<X64Register>(methodInProgress.Locals[dest].Type)
                {
                    Location = new StorageLocation<X64Register>(X64Register.Rax)
                });

                lowBlock.Instructions.Add(new LowInstruction(LowOp.Move, dest, methodInProgress.Locals.Count - 1, 0, 0));
            }
        }
    }
}

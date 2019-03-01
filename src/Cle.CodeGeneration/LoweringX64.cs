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
            foreach (var value in highMethod.Values)
            {
                lowMethod.Locals.Add(new LowLocal<X64Register>(value.Type));
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
            //   Return.
            var src = highMethod.Values[valueIndex];
            var dest = methodInProgress.Locals.Count;
            methodInProgress.Locals.Add(new LowLocal<X64Register>(src.Type)
                { Location = new StorageLocation<X64Register>(X64Register.Rax) });

            lowBlock.Instructions.Add(new LowInstruction(LowOp.Move, dest, valueIndex, 0, 0));
            lowBlock.Instructions.Add(new LowInstruction(LowOp.Return, 0, 0, 0, 0));
        }
    }
}

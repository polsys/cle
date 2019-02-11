using System;
using System.Diagnostics;
using Cle.CodeGeneration.Lir;
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

            if (highMethod.Body.BasicBlocks.Count > 1)
                throw new NotImplementedException("Multiple BBs");

            var lowMethod = new LowMethod<X64Register>();

            // Create locals for SSA values
            // Additional locals may be created by instructions
            foreach (var value in highMethod.Values)
            {
                lowMethod.Locals.Add(new LowLocal<X64Register>(value.Type));
            }

            // Convert each basic block
            lowMethod.Blocks.Add(ConvertBlock(highMethod.Body.BasicBlocks[0], highMethod, lowMethod));

            // TODO: Convert PHIs into copies

            return lowMethod;
        }

        private static LowBlock ConvertBlock(BasicBlock highBlock, CompiledMethod highMethod,
            LowMethod<X64Register> methodInProgress)
        {
            var lowBlock = new LowBlock();

            // Convert the instructions
            foreach (var inst in highBlock.Instructions)
            {
                switch (inst.Operation)
                {
                    case Opcode.Load:
                        lowBlock.Instructions.Add(new LowInstruction(LowOp.LoadInt, inst.Destination, 0, 0, inst.Left));
                        break;
                    case Opcode.Return:
                        // Convert "Return x" into "Move x -> return_reg, Return"
                        var src = highMethod.Values[(int)inst.Left];
                        var dest = methodInProgress.Locals.Count;
                        methodInProgress.Locals.Add(new LowLocal<X64Register>(src.Type)
                            { Location = new StorageLocation<X64Register>(X64Register.Rax) });

                        lowBlock.Instructions.Add(new LowInstruction(LowOp.Move, dest, (int)inst.Left, 0, 0));
                        lowBlock.Instructions.Add(new LowInstruction(LowOp.Return, 0, 0, 0, 0));
                        break;
                    default:
                        throw new NotImplementedException("Unimplemented opcode to lower");
                }
            }

            return lowBlock;
        }
    }
}

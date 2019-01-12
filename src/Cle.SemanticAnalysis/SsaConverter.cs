using System;
using System.Diagnostics;
using Cle.Common.TypeSystem;
using Cle.SemanticAnalysis.IR;
using JetBrains.Annotations;

namespace Cle.SemanticAnalysis
{
    /// <summary>
    /// <para>
    /// Provides a method for converting a <see cref="CompiledMethod"/> into Static Single Assignment form.
    /// This is required phase before code generation, but currently separate
    /// from <see cref="MethodCompiler"/> for easier debugging.
    /// </para>
    /// <para>
    /// A single instance can be used for successive SSA conversions, with the benefit of reduced allocations.
    /// </para>
    /// </summary>
    /// 
    /// <remarks>
    /// The algorithm used here is described by Braun et al. in "Simple and Efficient Construction
    /// of Static Single Assignment Form", proceedings of CC 2013 (Springer). The implementation
    /// tries to mirror the paper as closely as sensible.
    ///
    /// This algorithm could be used on the fly within <see cref="MethodCompiler"/>, but that is not done
    /// (at least for now) to make the algorithms easier to test and debug.
    /// </remarks>
    public class SsaConverter
    {
        private CompiledMethod _originalMethod;
        private CompiledMethod _newMethod;

        // Indexed by [original variable index, basic block index], values are offset by 1 so that 0 == undefined
        private int[][] _ssaValues;

        /// <summary>
        /// Returns a new <see cref="CompiledMethod"/> instance equivalent to the original
        /// <paramref name="method"/> after SSA conversion.
        /// </summary>
        /// <param name="method">The non-SSA method to convert.</param>
        [NotNull]
        public CompiledMethod ConvertToSsa([NotNull] CompiledMethod method)
        {
            if (method.Body is null)
                throw new ArgumentNullException(nameof(method), "Method must have body");

            // Reset the per-conversion data structures
            _originalMethod = method;
            _newMethod = new CompiledMethod(_originalMethod.FullName);
            _ssaValues = new int[method.Values.Count][];

            // Create value numbers for parameters and other locals with initial value
            for (var localIndex = 0; localIndex < method.Values.Count; localIndex++)
            {
                var local = method.Values[localIndex];
                if (!local.InitialValue.Equals(ConstantValue.Void()))
                {
                    WriteVariable(localIndex, 0, CreateValueNumber(local.Type, local.InitialValue));
                }
            }
            
            // Convert each basic block
            var blockGraphBuilder = new BasicBlockGraphBuilder();
            if (method.Body.BasicBlocks.Count > 1)
            {
                throw new NotImplementedException("Multiple BBs");
            }
            ConvertBlock(0, blockGraphBuilder.GetNewBasicBlock());

            // Return the converted method
            _newMethod.Body = blockGraphBuilder.Build();
            return _newMethod;
        }

        /// <summary>
        /// Converts the specified basic block into SSA form.
        /// </summary>
        /// <param name="blockIndex">The basic block index.</param>
        /// <param name="builder">The builder for the basic block.</param>
        private void ConvertBlock(int blockIndex, [NotNull] BasicBlockBuilder builder)
        {
            Debug.Assert(_originalMethod.Body != null);
            var block = _originalMethod.Body.BasicBlocks[blockIndex];
            
            // Unreachable blocks are easy to convert!
            if (block is null)
                return;

            foreach (var inst in block.Instructions)
            {
                if (inst.Operation == Opcode.Call)
                {
                    throw new NotImplementedException("Calls");
                }

                // Get SSA values for the operands
                var left = ReadVariable(inst.Left, blockIndex);
                var right = HasRightOperand(inst.Operation) ? ReadVariable(inst.Right, blockIndex) : 0;

                // CopyValue instructions now become simple SSA name updates, while others are still emitted
                if (inst.Operation == Opcode.CopyValue)
                {
                    WriteVariable(inst.Destination, blockIndex, left);
                }
                else
                {
                    // If the instruction produces a new value, we need an SSA number for it
                    var dest = 0;
                    if (DoesWriteValue(inst.Operation))
                    {
                        dest = CreateValueNumber(_originalMethod.Values[inst.Destination].Type, ConstantValue.Void());
                        WriteVariable(inst.Destination, blockIndex, dest);
                    }

                    builder.AppendInstruction(inst.Operation, left, right, dest);
                }
            }
        }

        /// <summary>
        /// Returns the SSA name for the given variable in the specified block.
        /// </summary>
        /// <param name="variableIndex">The original local variable index.</param>
        /// <param name="block">The basic block index.</param>
        private int ReadVariable(int variableIndex, int block)
        {
            // Value of 0 or completely unset means that the local is not written to in this block
            var localResult = _ssaValues[variableIndex]?[block] ?? 0;

            if (localResult > 0)
            {
                // We have a local value number
                return localResult - 1;
            }
            else
            {
                throw new NotImplementedException("Global value numbers");
            }
        }

        /// <summary>
        /// Associates a new SSA name with the given variable in the specified block.
        /// </summary>
        /// <param name="variableIndex">The original local variable index.</param>
        /// <param name="block">The basic block index.</param>
        /// <param name="newValue">The associated SSA name.</param>
        private void WriteVariable(int variableIndex, int block, int newValue)
        {
            // The values are stored in an on-demand created jagged array to save a bit of space
            if (_ssaValues[variableIndex] is null)
            {
                Debug.Assert(_originalMethod.Body != null);
                _ssaValues[variableIndex] = new int[_originalMethod.Body.BasicBlocks.Count];
            }

            // The values are offset by 1 to distinguish uninitialized values
            _ssaValues[variableIndex][block] = newValue + 1;
        }

        /// <summary>
        /// Creates a new local value with the given type and initial value, and returns its value number.
        /// </summary>
        private int CreateValueNumber(TypeDefinition type, ConstantValue initialValue)
        {
            return _newMethod.AddLocal(type, initialValue);
        }

        /// <summary>
        /// Returns whether the specified operation uses a right operand.
        /// </summary>
        private bool HasRightOperand(Opcode opcode)
        {
            switch (opcode)
            {
                case Opcode.Nop:
                case Opcode.Return:
                case Opcode.BranchIf:
                case Opcode.CopyValue:
                case Opcode.ArithmeticNegate:
                case Opcode.BitwiseNot:
                case Opcode.Call:
                    return false;
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
                    throw new NotImplementedException("Unimplemented opcode for HasRightOperand");
            }
        }

        /// <summary>
        /// Returns whether the specified operation writes to its destination operand.
        /// </summary>
        private bool DoesWriteValue(Opcode opcode)
        {
            switch (opcode)
            {
                case Opcode.Nop:
                case Opcode.Return:
                case Opcode.BranchIf:
                    return false;
                case Opcode.CopyValue:
                case Opcode.ArithmeticNegate:
                case Opcode.BitwiseNot:
                case Opcode.Call:
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
                    throw new NotImplementedException("Unimplemented opcode for DoesWriteValue");
            }
        }
    }
}

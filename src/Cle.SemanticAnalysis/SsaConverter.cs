using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
        private BasicBlockGraphBuilder _blockGraphBuilder;

        // Indexed by [original variable index, basic block index], values are offset by 1 so that 0 == undefined
        private ushort[][] _ssaValues;

        // Indexed by basic block index
        private bool[] _isSealed;
        private int[] _predecessorsSet;
        private List<(ushort variable, ushort phiValue)>[] _incompletePhis;

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

            // TODO: Pooling? Resizing?
            _ssaValues = new ushort[method.Values.Count][];
            var blockCount = method.Body.BasicBlocks.Count;
            _predecessorsSet = new int[blockCount];
            _incompletePhis = new List<(ushort, ushort)>[blockCount];
            _isSealed = new bool[blockCount];

            // Create value numbers for parameters.
            // Additionally, create a single value number that is used for all void returns.
            // (There is no sense in initializing a void value, let alone having several!)
            var voidIndex = -1;
            for (var localIndex = (ushort)0; localIndex < method.Values.Count; localIndex++)
            {
                var local = method.Values[localIndex];
                if (local.Flags.HasFlag(LocalFlags.Parameter))
                {
                    WriteVariable(localIndex, 0, CreateValueNumber(local.Type, local.Flags));
                }
                else if (local.Type.Equals(SimpleType.Void))
                {
                    if (voidIndex < 0)
                    {
                        voidIndex = CreateValueNumber(SimpleType.Void, LocalFlags.None);
                    }
                    WriteVariable(localIndex, 0, (ushort)voidIndex);
                }
            }
            
            // Convert each basic block
            _blockGraphBuilder = new BasicBlockGraphBuilder();
            for (var blockIndex = 0; blockIndex < method.Body.BasicBlocks.Count; blockIndex++)
            {
                ConvertBlock(blockIndex, _blockGraphBuilder.GetNewBasicBlock());
            }

            // Return the converted method
            _newMethod.Body = _blockGraphBuilder.Build();
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

            // If possible, seal this block
            TrySealBlock(blockIndex);

            // Perform value renaming on the instructions
            foreach (var inst in block.Instructions)
            {
                // Calls have an arbitrary number of operands
                if (inst.Operation == Opcode.Call)
                {
                    var originalCallInfo = _originalMethod.CallInfos[(int)inst.Left];

                    // Get SSA values for the operands
                    var parameterValues = new int[originalCallInfo.ParameterIndices.Length];
                    for (var i = 0; i < originalCallInfo.ParameterIndices.Length; i++)
                    {
                        parameterValues[i] = ReadVariable((ushort)originalCallInfo.ParameterIndices[i], blockIndex);
                    }

                    // Write the return value
                    var dest = CreateValueNumber(_originalMethod.Values[inst.Destination].Type, LocalFlags.None);
                    WriteVariable(inst.Destination, blockIndex, dest);

                    // Emit a call
                    var callIndex = _newMethod.AddCallInfo(originalCallInfo.CalleeIndex, parameterValues,
                        originalCallInfo.CalleeFullName);
                    builder.AppendInstruction(Opcode.Call, callIndex, 0, dest);

                    continue;
                }

                // Loads must be handled separately as Left is a constant value, not a value number
                if (inst.Operation == Opcode.Load)
                {
                    var dest = CreateValueNumber(_originalMethod.Values[inst.Destination].Type, LocalFlags.None);
                    WriteVariable(inst.Destination, blockIndex, dest);
                    builder.AppendInstruction(Opcode.Load, inst.Left, 0, dest);

                    continue;
                }

                // Get SSA values for the operands
                var left = ReadVariable((ushort)inst.Left, blockIndex);
                var right = HasRightOperand(inst.Operation) ? ReadVariable(inst.Right, blockIndex) : (ushort)0;

                // CopyValue instructions now become simple SSA name updates, while others are still emitted
                if (inst.Operation == Opcode.CopyValue)
                {
                    WriteVariable(inst.Destination, blockIndex, left);
                }
                else if (inst.Operation == Opcode.BranchIf)
                {
                    // We do not use CreateBranch because we override the target block.
                    // AlternativeSuccessor is set after this loop.
                    builder.AppendInstruction(Opcode.BranchIf, left, 0, 0);
                }
                else
                {
                    // If the instruction produces a new value, we need an SSA number for it
                    var dest = (ushort)0;
                    if (DoesWriteValue(inst.Operation))
                    {
                        dest = CreateValueNumber(_originalMethod.Values[inst.Destination].Type, LocalFlags.None);
                        WriteVariable(inst.Destination, blockIndex, dest);
                    }

                    builder.AppendInstruction(inst.Operation, left, right, dest);
                }
            }

            // Now this block is filled, and may have successors.
            // Update the predecessors list to match this.
            if (block.DefaultSuccessor != -1)
            {
                builder.SetSuccessor(block.DefaultSuccessor);
                _predecessorsSet[block.DefaultSuccessor]++;
                TrySealBlock(block.DefaultSuccessor);
            }
            if (block.AlternativeSuccessor != -1)
            {
                builder.SetAlternativeSuccessor(block.AlternativeSuccessor);
                _predecessorsSet[block.AlternativeSuccessor]++;
                TrySealBlock(block.AlternativeSuccessor);
            }
        }

        /// <summary>
        /// Returns the SSA name for the given variable in the specified block.
        /// </summary>
        /// <param name="variableIndex">The original local variable index.</param>
        /// <param name="blockIndex">The basic block index.</param>
        private ushort ReadVariable(ushort variableIndex, int blockIndex)
        {
            // Value of 0 or completely unset means that the local is not written to in this block
            var localResult = _ssaValues[variableIndex]?[blockIndex] ?? 0;

            if (localResult > 0)
            {
                // We have a local value number
                return (ushort)(localResult - 1);
            }
            else
            {
                Debug.Assert(_originalMethod.Body != null);
                var block = _originalMethod.Body.BasicBlocks[blockIndex];
                Debug.Assert(block != null);

                if (!_isSealed[blockIndex])
                {
                    // Incomplete CFG
                    // Create a value for the Phi and set the Phi aside to be created when the block is sealed
                    var phiType = _originalMethod.Values[variableIndex].Type;
                    var phiValueNumber = CreateValueNumber(phiType, LocalFlags.None);
                    WriteVariable(variableIndex, blockIndex, phiValueNumber);

                    if (_incompletePhis[blockIndex] is null)
                    {
                        _incompletePhis[blockIndex] = new List<(ushort, ushort)>();
                    }
                    _incompletePhis[blockIndex].Add((variableIndex, phiValueNumber));

                    return phiValueNumber;
                }
                else if (block.Predecessors.Count == 1)
                {
                    // The trivial case: no phi needed
                    return ReadVariable(variableIndex, block.Predecessors[0]);
                }
                else
                {
                    // Complete CFG
                    // Before recursing, write the Phi value to break cycles
                    var phiType = _originalMethod.Values[variableIndex].Type;
                    var phiValueNumber = CreateValueNumber(phiType, LocalFlags.None);
                    WriteVariable(variableIndex, blockIndex, phiValueNumber);

                    var operands = GetPhiOperands(variableIndex, blockIndex);
                    if (IsTrivialPhi(operands))
                    {
                        // If there is exactly one unique operand, skip generating a Phi.
                        // The value created earlier will be left unused.
                        var trivialValue = (ushort)operands[0];
                        WriteVariable(variableIndex, blockIndex, trivialValue);
                        return trivialValue;
                    }
                    else
                    {
                        _blockGraphBuilder.GetBuilderByBlockIndex(blockIndex).AddPhi(phiValueNumber, operands);
                        return phiValueNumber;
                    }
                }
            }
        }

        /// <summary>
        /// Returns the list of operands for the given Phi, with duplicates removed.
        /// </summary>
        private ImmutableList<int> GetPhiOperands(ushort variableIndex, int blockIndex)
        {
            Debug.Assert(_originalMethod.Body != null);
            var block = _originalMethod.Body.BasicBlocks[blockIndex];
            Debug.Assert(block != null);

            var operands = ImmutableList<int>.Empty.ToBuilder();
            foreach (var predecessor in block.Predecessors)
            {
                operands.Add(ReadVariable(variableIndex, predecessor));
            }

            return operands.ToImmutable();
        }

        /// <summary>
        /// Returns whether all the operands are the same.
        /// </summary>
        private static bool IsTrivialPhi(ImmutableList<int> operands)
        {
            Debug.Assert(operands.Count > 0);

            var first = operands[0];
            foreach (var value in operands)
            {
                if (value != first)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Associates a new SSA name with the given variable in the specified block.
        /// </summary>
        /// <param name="variableIndex">The original local variable index.</param>
        /// <param name="block">The basic block index.</param>
        /// <param name="newValue">The associated SSA name.</param>
        private void WriteVariable(ushort variableIndex, int block, ushort newValue)
        {
            // The values are stored in an on-demand created jagged array to save a bit of space
            if (_ssaValues[variableIndex] is null)
            {
                Debug.Assert(_originalMethod.Body != null);
                _ssaValues[variableIndex] = new ushort[_originalMethod.Body.BasicBlocks.Count];
            }

            // The values are offset by 1 to distinguish uninitialized values
            _ssaValues[variableIndex][block] = (ushort)(newValue + 1);
        }

        /// <summary>
        /// Seals the block if it is not yet sealed and has all its predecessors set.
        /// </summary>
        private void TrySealBlock(int blockIndex)
        {
            Debug.Assert(_originalMethod.Body != null);

            // TODO Perf: making the precondition check inline-able might have a positive impact
            if (!_isSealed[blockIndex] &&
                _originalMethod.Body.BasicBlocks[blockIndex]?.Predecessors.Count == _predecessorsSet[blockIndex])
            {
                _isSealed[blockIndex] = true;

                // Add incomplete Phi operands
                if (_incompletePhis[blockIndex] != null)
                {
                    foreach (var (variable, destination) in _incompletePhis[blockIndex])
                    {
                        // TODO: There might be some opportunity for optimizing away trivial Phis (see Braun et al.)
                        var operands = GetPhiOperands(variable, blockIndex);
                        _blockGraphBuilder.GetBuilderByBlockIndex(blockIndex).AddPhi(destination, operands);
                    }
                }
            }
        }

        /// <summary>
        /// Creates a new local value with the given type and flags, and returns its value number.
        /// </summary>
        private ushort CreateValueNumber(TypeDefinition type, LocalFlags flags)
        {
            return _newMethod.AddLocal(type, flags);
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

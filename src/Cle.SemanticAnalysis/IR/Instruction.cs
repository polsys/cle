﻿using System;

namespace Cle.SemanticAnalysis.IR
{
    /// <summary>
    /// A single intermediate representation (IR) instruction with operation code and three operands
    /// (two sources and destination).
    /// </summary>
    public readonly struct Instruction : IEquatable<Instruction>
    {
        /// <summary>
        /// The first parameter to the operation.
        /// The interpretation of this field depends on the operation.
        /// </summary>
        public readonly ulong Left;

        /// <summary>
        /// The second parameter to the operation.
        /// The interpretation of this field depends on the operation.
        /// </summary>
        public readonly ushort Right;

        /// <summary>
        /// The destination local index of the operation.
        /// </summary>
        public readonly ushort Destination;

        /// <summary>
        /// The operation code.
        /// </summary>
        public readonly Opcode Operation;

        public Instruction(Opcode operation, ulong left, ushort right, ushort destination)
        {
            Operation = operation;
            Left = left;
            Right = right;
            Destination = destination;
        }

        public override bool Equals(object obj)
        {
            return obj is Instruction other && Equals(other);
        }

        public bool Equals(Instruction other)
        {
            return Operation == other.Operation &&
                   Left == other.Left &&
                   Right == other.Right &&
                   Destination == other.Destination;
        }

        public override int GetHashCode()
        {
            var hashCode = -175388878;
            hashCode = hashCode * -1521134295 + Operation.GetHashCode();
            hashCode = hashCode * -1521134295 + Left.GetHashCode();
            hashCode = hashCode * -1521134295 + Right.GetHashCode();
            hashCode = hashCode * -1521134295 + Destination.GetHashCode();
            return hashCode;
        }

        public static bool operator ==(Instruction left, Instruction right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Instruction left, Instruction right)
        {
            return !(left == right);
        }
    }

    /// <summary>
    /// Intermediate representation (IR) operations.
    /// </summary>
    public enum Opcode
    {
        /// <summary>
        /// No operation.
        /// This operation is ignored in code generation.
        /// The source and destination operands are ignored.
        /// </summary>
        Nop,
        /// <summary>
        /// Loads the constant stored in the left operand to the local indexed by destination.
        /// The constant bits are interpreted according to the local type.
        /// </summary>
        Load,
        /// <summary>
        /// Exit the method and return the value indexed by the left operand.
        /// </summary>
        Return,
        /// <summary>
        /// If the boolean value indexed by the left operand is true, jump to the alternative successor block.
        /// Else, jump to the default successor block.
        /// </summary>
        BranchIf,
        /// <summary>
        /// Copies the local value indexed by the left operand to the destination operand.
        /// </summary>
        CopyValue,
        /// <summary>
        /// Adds the local values indexed by left and right operands and stores the result in the destination index.
        /// </summary>
        Add,
        /// <summary>
        /// Subtracts the right local from the left local and stores the result in the destination index.
        /// </summary>
        Subtract,
        /// <summary>
        /// Multiplies the local values indexed by left and right operands and stores the result in the destination index.
        /// </summary>
        Multiply,
        /// <summary>
        /// Divides the left local by the right local and stores the result in the destination index.
        /// </summary>
        Divide,
        /// <summary>
        /// Divides the left local by the right local and stores the remainder in the destination index.
        /// </summary>
        Modulo,
        /// <summary>
        /// Negates the numeric value indexed by the left operand and stores the result in the destination index.
        /// </summary>
        ArithmeticNegate,
        /// <summary>
        /// Performs bitwise AND on the left and right locals and stores the result in the destination index.
        /// If the arguments are Booleans, this is equivalent to a logical AND.
        /// </summary>
        BitwiseAnd,
        /// <summary>
        /// Performs bitwise NOT on the left and right locals and stores the result in the destination index.
        /// If the argument is Boolean, this is equivalent to a logical NOT.
        /// </summary>
        BitwiseNot,
        /// <summary>
        /// Performs bitwise OR on the left and right locals and stores the result in the destination index.
        /// If the arguments are Booleans, this is equivalent to a logical OR.
        /// </summary>
        BitwiseOr,
        /// <summary>
        /// Performs bitwise XOR on the left and right locals and stores the result in the destination index.
        /// If the arguments are Booleans, this is equivalent to a logical OR.
        /// </summary>
        BitwiseXor,
        /// <summary>
        /// Shifts left the bits of the left local by the right local and stores the result in the destination index.
        /// The right local is masked to the operand size.
        /// </summary>
        ShiftLeft,
        /// <summary>
        /// Shifts right the bits of the left local by the right local and stores the result in the destination index.
        /// The right local is masked to the operand size.
        /// </summary>
        ShiftRight,
        /// <summary>
        /// Stores the result of (left local) &lt; (right local) to the destination index.
        /// </summary>
        Less,
        /// <summary>
        /// Stores the result of (left local) &lt;= (right local) to the destination index.
        /// </summary>
        LessOrEqual,
        /// <summary>
        /// Stores the result of (left local) == (right local) to the destination index.
        /// </summary>
        Equal,
        /// <summary>
        /// Performs the method call described by the call info indexed by the left operand
        /// and stores the result in the destination local.
        /// </summary>
        Call,
    }
}

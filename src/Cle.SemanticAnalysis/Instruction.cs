﻿using System;

namespace Cle.SemanticAnalysis
{
    /// <summary>
    /// A single intermediate representation (IR) instruction with operation code and three operands
    /// (two sources and destination).
    /// </summary>
    public readonly struct Instruction : IEquatable<Instruction>
    {
        /// <summary>
        /// The operation code.
        /// </summary>
        public readonly Opcode Operation;

        /// <summary>
        /// The first parameter to the operation.
        /// The interpretation of this field depends on the operation.
        /// </summary>
        public readonly int Left;

        /// <summary>
        /// The second parameter to the operation.
        /// The interpretation of this field depends on the operation.
        /// </summary>
        public readonly int Right;

        /// <summary>
        /// The destination local index of the operation.
        /// </summary>
        public readonly int Destination;

        public Instruction(Opcode operation, int left, int right, int destination)
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
        /// Exit the method and return the value indexed by the left operand.
        /// </summary>
        Return,
        /// <summary>
        /// If the boolean value indexed by the left operand is true, jump to the alternative successor block.
        /// Else, jump to the default successor block.
        /// </summary>
        BranchIf,
    }
}

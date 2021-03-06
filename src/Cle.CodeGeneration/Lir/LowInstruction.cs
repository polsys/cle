﻿using System;

namespace Cle.CodeGeneration.Lir
{
    /// <summary>
    /// LIR instruction with opcode and four operands (three sources, one destination).
    /// </summary>
    internal readonly struct LowInstruction : IEquatable<LowInstruction>
    {
        public readonly LowOp Op;
        public readonly int Dest;
        public readonly int Left;
        public readonly int Right;
        public readonly ulong Data;

        public LowInstruction(LowOp op, int dest, int left, int right, ulong data)
        {
            Op = op;
            Dest = dest;
            Left = left;
            Right = right;
            Data = data;
        }

        /// <summary>
        /// Returns whether <see cref="Left"/> refers to a local.
        /// </summary>
        public bool UsesLeft => Op > LowOp.LoadInt && Op < LowOp.SetIfEqual || Op == LowOp.Return;

        /// <summary>
        /// Returns whether <see cref="Right"/> refers to a local.
        /// </summary>
        public bool UsesRight => Op == LowOp.Swap || Op == LowOp.Compare ||
            (Op >= LowOp.IntegerAdd && Op <= LowOp.BitwiseXor);

        /// <summary>
        /// Returns whether <see cref="Dest"/> refers to a local.
        /// </summary>
        public bool UsesDest => Op == LowOp.LoadInt || Op == LowOp.Move ||
                                (Op >= LowOp.IntegerAdd && Op <= LowOp.BitwiseNot) ||
                                (Op >= LowOp.SetIfEqual && Op <= LowOp.SetIfGreaterOrEqual) || 
                                Op == LowOp.Call || Op == LowOp.CallImported;

        public override bool Equals(object obj)
        {
            return obj is LowInstruction instruction && Equals(instruction);
        }

        public bool Equals(LowInstruction other)
        {
            return Op == other.Op &&
                   Dest == other.Dest &&
                   Left == other.Left &&
                   Right == other.Right &&
                   Data == other.Data;
        }

        public override int GetHashCode()
        {
            var hashCode = 1623468473;
            hashCode = hashCode * -1521134295 + Op.GetHashCode();
            hashCode = hashCode * -1521134295 + Dest.GetHashCode();
            hashCode = hashCode * -1521134295 + Left.GetHashCode();
            hashCode = hashCode * -1521134295 + Right.GetHashCode();
            hashCode = hashCode * -1521134295 + Data.GetHashCode();
            return hashCode;
        }

        public static bool operator ==(LowInstruction left, LowInstruction right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(LowInstruction left, LowInstruction right)
        {
            return !(left == right);
        }

        public override string ToString()
        {
            return $"{Op} {Left} {Right} {Data} -> {Dest}";
        }
    }

    internal enum LowOp
    {
        Invalid,
        /// <summary>
        /// No operation.
        /// </summary>
        Nop,

        // Memory operations

        /// <summary>
        /// Loads the integer value stored in Data to the Dest local.
        /// The Left operand specifies the data size in bytes.
        /// </summary>
        LoadInt,
        /// <summary>
        /// Moves the value in Left to Dest.
        /// </summary>
        Move,
        /// <summary>
        /// Swaps Left and Right.
        /// </summary>
        Swap,

        // Arithmetic - binary operations

        /// <summary>
        /// Sums the Left and Right locals and stores the result in Dest local.
        /// If Right == -1, a constant in Data is used instead (at most 4 bytes, sign-extended to 8 if necessary).
        /// </summary>
        IntegerAdd,
        /// <summary>
        /// Subtracts the Right local from the Left local and stores the result in Dest local.
        /// If Right == -1, a constant in Data is used instead (at most 4 bytes, sign-extended to 8 if necessary).
        /// </summary>
        IntegerSubtract,
        /// <summary>
        /// Multiplies the Left and Right locals and stores the result in Dest local.
        /// If Right == -1, a constant in Data is used instead (at most 4 bytes, sign-extended to 8 if necessary).
        /// </summary>
        IntegerMultiply,
        /// <summary>
        /// Divides the Left local by the Right local and stores the result in Dest local.
        /// The signedness of the division depends on the type of the locals.
        /// The Left and Dest locals must be stored in the proper location.
        /// </summary>
        IntegerDivide,
        /// <summary>
        /// As <see cref="IntegerDivide"/>.
        /// </summary>
        IntegerModulo,
        /// <summary>
        /// Shifts the Left local left by the amount specified in the Right local (modulo operand size)
        /// and stores the result in the Dest local.
        /// The Right local must be stored in the platform-specified location.
        /// If Right == -1, the shift amount is a constant stored in Data instead.
        /// </summary>
        ShiftLeft,
        /// <summary>
        /// Shifts the Left local right by the amount specified in the Right local (modulo operand size),
        /// propagating the sign bit, and stores the result in the Dest local.
        /// The Right local must be stored in the platform-specified location.
        /// If Right == -1, the shift amount is a constant stored in Data instead.
        /// </summary>
        ShiftArithmeticRight,
        /// <summary>
        /// Performs a bitwise AND of the Left and Right locals and stores the result in Dest local.
        /// </summary>
        BitwiseAnd,
        /// <summary>
        /// Performs a bitwise OR of the Left and Right locals and stores the result in Dest local.
        /// </summary>
        BitwiseOr,
        /// <summary>
        /// Performs a bitwise XOR of the Left and Right locals and stores the result in Dest local.
        /// </summary>
        BitwiseXor,

        // Arithmetic - unary operations

        /// <summary>
        /// Stores the negated value of the Left local in the Dest local.
        /// </summary>
        IntegerNegate,
        /// <summary>
        /// Stores the bitwise NOT of the Left local in the Dest local.
        /// </summary>
        BitwiseNot,

        // Conditions

        /// <summary>
        /// Compares Left and Right and stores the result in processor flags.
        /// If Right == -1, the value of the Left local is compared against Data (width specified by Left,
        /// at most 4 bytes, sign-extended to 8 if necessary).
        /// </summary>
        Compare,
        /// <summary>
        /// Sets processor flags according to the contents of Left.
        /// </summary>
        Test,
        /// <summary>
        /// Sets Dest local to 1 if the previous comparison has set the Equal/Zero flag.
        /// </summary>
        SetIfEqual,
        /// <summary>
        /// Sets Dest local to 1 if the previous comparison has not set the Equal/Zero flag.
        /// </summary>
        SetIfNotEqual,
        /// <summary>
        /// Sets Dest local to 1 if the previous comparison has set the Less flag.
        /// </summary>
        SetIfLess,
        /// <summary>
        /// Sets Dest local to 1 if the previous comparison has set the Equal/Zero or the Less flag.
        /// </summary>
        SetIfLessOrEqual,
        /// <summary>
        /// Sets Dest local to 1 if the previous comparison has set the Greater flag.
        /// </summary>
        SetIfGreater,
        /// <summary>
        /// Sets Dest local to 1 if the previous comparison has set the Equal/Zero or the Greater flag.
        /// </summary>
        SetIfGreaterOrEqual,

        // Control flow

        /// <summary>
        /// Jumps unconditionally to the low block indexed by Dest.
        /// </summary>
        Jump,
        /// <summary>
        /// Jumps to the low block indexed by Dest if the Equal/Zero flag is set.
        /// </summary>
        JumpIfEqual,
        /// <summary>
        /// Jumps to the low block indexed by Dest if the Equal/Zero flag is not set.
        /// </summary>
        JumpIfNotEqual,
        /// <summary>
        /// Jumps to the low block indexed by Dest if the Less flag is set.
        /// </summary>
        JumpIfLess,
        /// <summary>
        /// Jumps to the low block indexed by Dest if the Less or Equal/Zero flag is set.
        /// </summary>
        JumpIfLessOrEqual,
        /// <summary>
        /// Jumps to the low block indexed by Dest if the Greater flag is set.
        /// </summary>
        JumpIfGreater,
        /// <summary>
        /// Jumps to the low block indexed by Dest if the Greater or Equal/Zero flag is set.
        /// </summary>
        JumpIfGreaterOrEqual,
        /// <summary>
        /// Calls the Clé native method indexed by Data.
        /// The Left operand contains the call info index for disassembly purposes.
        /// The return value is stored in Dest - this local must be in the fixed return location.
        /// </summary>
        Call,
        /// <summary>
        /// Calls the external method indexed by Data.
        /// The Left operand contains the call info index for disassembly purposes.
        /// The return value is stored in Dest - this local must be in the fixed return location.
        /// </summary>
        CallImported,
        /// <summary>
        /// Exits the method.
        /// The left operand is returned - this local must also be in the fixed return location.
        /// </summary>
        Return,
    }
}

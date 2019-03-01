namespace Cle.CodeGeneration.Lir
{
    /// <summary>
    /// LIR instruction with opcode and four operands (three sources, one destination).
    /// </summary>
    internal readonly struct LowInstruction
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

        public bool UsesLeft => Op > LowOp.LoadInt && Op < LowOp.SetIfEqual;
        public bool UsesRight => Op == LowOp.Compare || Op == LowOp.IntegerAdd;

        public bool UsesDest => Op == LowOp.LoadInt || Op == LowOp.Move ||
                                Op == LowOp.IntegerAdd ||
                                Op == LowOp.SetIfEqual || Op == LowOp.Jump ||
                                Op == LowOp.JumpIfEqual || Op == LowOp.JumpIfNotEqual;
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

        // Arithmetic

        /// <summary>
        /// Sums the Left and Right locals and stores the result in Dest local.
        /// </summary>
        IntegerAdd,

        // Conditions

        /// <summary>
        /// Compares Left and Right and stores the result in processor flags.
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
        /// Exits the method.
        /// Has no operands - the return value is assumed to be in the correct location.
        /// </summary>
        Return,
    }
}

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
        /// Exits the method.
        /// Has no operands - the return value is assumed to be in the correct location.
        /// </summary>
        Return,
    }
}

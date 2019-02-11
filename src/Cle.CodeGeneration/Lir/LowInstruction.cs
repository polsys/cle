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
        /// Exits the method.
        /// Has no operands - the return value is assumed to be in the correct location.
        /// </summary>
        Return,
    }
}

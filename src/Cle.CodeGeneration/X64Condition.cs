namespace Cle.CodeGeneration
{
    /// <summary>
    /// Condition codes for conditional instructions.
    /// The enum values match x64 binary encodings for the conditions.
    /// </summary>
    internal enum X64Condition
    {
        Overflow = 0,
        Equal = 0b0100,
        NotEqual = 0b0101,
        Less = 0b1100,
        GreaterOrEqual = 0b1101,
        LessOrEqual = 0b1110,
        Greater = 0b1111,
    }
}

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
    }
}

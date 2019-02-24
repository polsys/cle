namespace Cle.CodeGeneration
{
    /// <summary>
    /// Information for later correcting an object file offset.
    /// </summary>
    internal readonly struct Fixup
    {
        /// <summary>
        /// The type (typically, instruction class) of the reference.
        /// </summary>
        public readonly FixupType Type;

        /// <summary>
        /// A tag that identifies the pointed to object.
        /// </summary>
        public readonly int Tag;

        /// <summary>
        /// The object file position where the fixup will be applied.
        /// </summary>
        public readonly int Position;

        public Fixup(FixupType type, int tag, int position)
        {
            Type = type;
            Tag = tag;
            Position = position;
        }
    }
    
    internal enum FixupType
    {
        Invalid,
        /// <summary>
        /// This is a relative jump instruction.
        /// Position points to the displacement field.
        /// </summary>
        RelativeJump
    }
}

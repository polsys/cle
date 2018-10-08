using System;

namespace Cle.Common
{
    /// <summary>
    /// Represents a position within a source file.
    /// </summary>
    public readonly struct TextPosition : IEquatable<TextPosition>
    {
        /// <summary>
        /// The source code row.
        /// The first row is numbered 1.
        /// </summary>
        public readonly int Line;

        /// <summary>
        /// Zero-based byte offset from the first character of the row.
        /// For ASCII text this matches the column exactly, but UTF-8 text needs special handling.
        /// </summary>
        public readonly int ByteInLine;

        /// <summary>
        /// Number of bytes from the start of file.
        /// </summary>
        public readonly int ByteInFile;

        public TextPosition(int byteInFile, int row, int byteInRow)
        {
            Line = row;
            ByteInLine = byteInRow;
            ByteInFile = byteInFile;
        }

        public override bool Equals(object obj)
        {
            return obj is TextPosition position && Equals(position);
        }

        public bool Equals(TextPosition other)
        {
            return Line == other.Line &&
                   ByteInLine == other.ByteInLine &&
                   ByteInFile == other.ByteInFile;
        }

        public override int GetHashCode()
        {
            var hashCode = 185157821;
            hashCode = hashCode * -1521134295 + Line.GetHashCode();
            hashCode = hashCode * -1521134295 + ByteInLine.GetHashCode();
            hashCode = hashCode * -1521134295 + ByteInFile.GetHashCode();
            return hashCode;
        }

        public static bool operator ==(TextPosition left, TextPosition right) => left.Equals(right);

        public static bool operator !=(TextPosition left, TextPosition right) => !left.Equals(right);

        public override string ToString()
        {
            return $"Line {Line}, byte {ByteInLine} (in file: {ByteInFile})";
        }
    }
}

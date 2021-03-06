﻿namespace Cle.Common
{
    /// <summary>
    /// Represents a compilation diagnostic code along with source position and additional information.
    /// </summary>
    public class Diagnostic
    {
        /// <summary>
        /// Gets the diagnostic code.
        /// </summary>
        public DiagnosticCode Code { get; }

        /// <summary>
        /// The content of this value depends on the diagnostic code.
        /// This may be null.
        /// </summary>
        public string? Actual { get; }

        /// <summary>
        /// The content of this value depends on the diagnostic code.
        /// This may be null.
        /// </summary>
        public string? Expected { get; }

        /// <summary>
        /// Gets whether the diagnostic code is classified as an error or not.
        /// </summary>
        public bool IsError =>
            Code < DiagnosticCode.ParseWarningStart ||
            Code >= DiagnosticCode.SemanticErrorStart && Code < DiagnosticCode.SemanticWarningStart ||
            Code >= DiagnosticCode.BackendErrorStart && Code < DiagnosticCode.BackendWarningStart;

        /// <summary>
        /// Gets the source code position where the diagnostic occurred.
        /// </summary>
        public TextPosition Position { get; }

        /// <summary>
        /// Gets the name of the file where the diagnostic occurred.
        /// This may be null.
        /// </summary>
        public string? Filename { get; }

        /// <summary>
        /// Gets the name of the module containing the source file.
        /// </summary>
        public string Module { get; }

        /// <summary>
        /// Creates a compilation diagnostic with diagnostic code and source position.
        /// </summary>
        /// <param name="code">The diagnostic code.</param>
        /// <param name="position">The position within the source file.</param>
        /// <param name="filename">The source file name (optional if no associated file).</param>
        /// <param name="moduleName">The module name.</param>
        /// <param name="actual">Optional actual value encountered.</param>
        /// <param name="expected">Optional expected value.</param>
        public Diagnostic(DiagnosticCode code, TextPosition position,
            string? filename, string moduleName,
            string? actual, string? expected)
        {
            Code = code;
            Position = position;
            Filename = filename;
            Module = moduleName;
            Actual = actual;
            Expected = expected;
        }

        public override string ToString()
        {
            return $"{Code} (line {Position.Line}, byte {Position.ByteInLine})";
        }
    }
}

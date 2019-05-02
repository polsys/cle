namespace Cle.Common
{
    /// <summary>
    /// The interface implemented by types that collect compilation diagnostics.
    /// </summary>
    public interface IDiagnosticSink
    {
        /// <summary>
        /// Adds the specified diagnostic at given location and no other information.
        /// </summary>
        void Add(DiagnosticCode code, TextPosition position);

        /// <summary>
        /// Adds the specified diagnostic at given location with actual (unexpected) value.
        /// </summary>
        void Add(DiagnosticCode code, TextPosition position, string? actual);

        /// <summary>
        /// Adds the specified diagnostic at given location with actual and expected value.
        /// </summary>
        void Add(DiagnosticCode code, TextPosition position, string? actual, string? expected);
    }
}

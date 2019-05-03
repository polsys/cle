using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using Cle.SemanticAnalysis;
using Cle.SemanticAnalysis.IR;

namespace Cle.Compiler
{
    /// <summary>
    /// Implements compiler debug logging such as dumping the IR of user-specified methods in various phases.
    /// </summary>
    internal class DebugLogger
    {
        private readonly string? _pattern;

        /// <summary>
        /// Gets the internal writer, which may be null.
        /// </summary>
        public TextWriter? Writer { get; }

        /// <summary>
        /// Creates a logger.
        /// If <paramref name="writer"/> is null, instance methods are no-ops.
        /// </summary>
        /// <param name="writer">
        /// An optional writer for the debug logging. If null, no output is produced.
        /// </param>
        /// <param name="methodPattern">
        /// A string that is searched for in method names.
        /// A null pattern matches no method and "*" or empty matches all methods.
        /// </param>
        public DebugLogger(TextWriter? writer, string? methodPattern)
        {
            Writer = writer;

            if (methodPattern is null)
            {
                WriteLine("No method pattern specified: no methods will be dumped.");
            }
            // Empty string is contained in every string
            _pattern = methodPattern == "*" ? "" : methodPattern;
        }

        /// <summary>
        /// Returns true iff the method name contains the pattern string and logging is enabled. 
        /// </summary>
        /// <param name="methodName">The full name of the method to match.</param>
        public bool ShouldLog(string methodName)
        {
            return Writer != null && _pattern != null &&
                methodName.IndexOf(_pattern, StringComparison.InvariantCultureIgnoreCase) != -1;
        }

        /// <summary>
        /// If <paramref name="method"/> matches the pattern, the method will be disassembled
        /// to the output writer.
        /// </summary>
        /// <param name="method">The method IR.</param>
        public void DumpMethod(CompiledMethod method)
        {
            if (!ShouldLog(method.FullName))
                return;
            Debug.Assert(Writer is object);

            // Write the full name as a comment
            Writer.Write("; ");
            Writer.WriteLine(method.FullName);

            // Disassemble the method
            if (method.Body is null)
            {
                Writer.WriteLine("; (Method has no body)");
                Writer.WriteLine();
            }
            else
            {
                var builder = new StringBuilder();
                MethodDisassembler.Disassemble(method, builder);

                Writer.Write(builder);
            }
            Writer.WriteLine();
        }

        /// <summary>
        /// Writes a header surrounded by padding and a header sign.
        /// </summary>
        /// <param name="header">The header text.</param>
        public void WriteHeader(string header)
        {
            if (Writer != null)
            {
                Writer.WriteLine();
                Writer.WriteLine();
                Writer.Write("## ");
                Writer.WriteLine(header);
                Writer.WriteLine();
            }
        }

        /// <summary>
        /// Writes the specified text followed by a newline.
        /// </summary>
        public void WriteLine(string text)
        {
            Writer?.WriteLine(text);
        }
    }
}

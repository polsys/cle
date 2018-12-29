﻿using System;
using System.IO;
using System.Text;
using Cle.SemanticAnalysis;
using Cle.SemanticAnalysis.IR;
using JetBrains.Annotations;

namespace Cle.Compiler
{
    /// <summary>
    /// Implements compiler debug logging such as dumping the IR of user-specified methods in various phases.
    /// </summary>
    internal class DebugLogger
    {
        [CanBeNull] private readonly TextWriter _writer;
        [CanBeNull] private readonly string _pattern;

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
        public DebugLogger([CanBeNull] TextWriter writer, [CanBeNull] string methodPattern)
        {
            _writer = writer;

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
        public bool ShouldLog([NotNull] string methodName)
        {
            return _writer != null && _pattern != null &&
                methodName.IndexOf(_pattern, StringComparison.InvariantCultureIgnoreCase) != -1;
        }

        /// <summary>
        /// If <paramref name="methodName"/> matches the method name, the method will be disassembled
        /// to the output writer.
        /// </summary>
        /// <param name="method">The method IR.</param>
        public void DumpMethod([NotNull] CompiledMethod method)
        {
            if (!ShouldLog(method.FullName))
                return;

            // Write the full name as a comment
            _writer.Write("; ");
            _writer.WriteLine(method.FullName);

            // Disassemble the method
            if (method.Body is null)
            {
                _writer.WriteLine("; (Method has no body)");
                _writer.WriteLine();
            }
            else
            {
                var builder = new StringBuilder();
                MethodDisassembler.Disassemble(method, builder);

                _writer.Write(builder);
            }
            _writer.WriteLine();
        }

        /// <summary>
        /// Writes a header surrounded by padding and a header sign.
        /// </summary>
        /// <param name="header">The header text.</param>
        public void WriteHeader([NotNull] string header)
        {
            if (_writer != null)
            {
                _writer.WriteLine();
                _writer.WriteLine();
                _writer.Write("## ");
                _writer.WriteLine(header);
                _writer.WriteLine();
            }
        }

        /// <summary>
        /// Writes the specified text followed by a newline.
        /// </summary>
        public void WriteLine([NotNull] string text)
        {
            if (_writer != null)
            {
                _writer.WriteLine(text);
            }
        }
    }
}
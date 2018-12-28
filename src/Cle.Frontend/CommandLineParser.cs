using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Cle.Compiler;
using CommandLine;
using JetBrains.Annotations;

namespace Cle.Frontend
{
    /// <summary>
    /// Provides static methods for handling the command line interface.
    /// </summary>
    internal static class CommandLineParser
    {
        /// <summary>
        /// Tries to parse the command line arguments.
        /// Returns false if parsing fails or no compilation is to be done.
        /// </summary>
        /// <param name="args">The command line arguments passed to the executable.</param>
        /// <param name="output">A writer for any output such as help.</param>
        /// <param name="options">The parsed compilation options, if successful.</param>
        public static bool TryParseArguments(
            [NotNull, ItemNotNull] IEnumerable<string> args,
            [NotNull] TextWriter output,
            [CanBeNull] out CompilationOptions options)
        {
            var parser = new CommandLine.Parser(config =>
            {
                config.HelpWriter = output;
            });
            var result = parser.ParseArguments<CommandLineOptions>(args);

            if (result is Parsed<CommandLineOptions> parsed)
            {
                // Convert the options into compilation options
                options = new CompilationOptions(
                    parsed.Value.MainModule.FirstOrDefault() ?? ".",
                    debugPattern: parsed.Value.DumpRegex);

                return true;
            }
            else
            {
                options = null;
                return false;
            }
        }
    }
}

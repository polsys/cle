using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Cle.Compiler;
using CommandLine;

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
            IEnumerable<string> args,
            TextWriter output,
            [NotNullWhen(true)] out CompilationOptions? options)
        {
            var parser = new CommandLine.Parser(config =>
            {
                config.HelpWriter = output;
            });
            var result = parser.ParseArguments<CommandLineOptions>(args);

            if (result is Parsed<CommandLineOptions> parsed)
            {
                // Handle the case where multiple main modules are specified by ourselves.
                // We could also set an Value attribute property Max=1, but the error message would be:
                //   "A sequence value not bound to option name is defined with few items than required."
                // So yeah, maybe it is better to handle that by ourselves.
                Debug.Assert(parsed.Value.MainModule != null);

                var mainModules = new List<string>(parsed.Value.MainModule);
                if (mainModules.Count > 1)
                {
                    output.WriteLine("ERROR(S):");
                    output.WriteLine("More than one main module specified.");

                    options = null;
                    return false;
                }

                // Convert the options into compilation options
                options = new CompilationOptions(
                    mainModules.Count == 0 ? "." : mainModules[0],
                    debugPattern: parsed.Value.DumpRegex,
                    emitDisassembly: parsed.Value.Disassembly);

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

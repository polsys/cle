using System.Collections.Generic;
using System.Diagnostics;
using Cle.Common;
using Cle.Parser;
using JetBrains.Annotations;

namespace Cle.Compiler
{
    /// <summary>
    /// Provides the compiler entry point.
    /// </summary>
    public static class CompilerDriver
    {
        /// <summary>
        /// Compiles the specified module and its dependencies.
        /// Returns compilation diagnostics and statistics.
        /// </summary>
        /// <param name="mainModule">
        /// The main module name. This module will be located and its dependencies will be added to the compilation.
        /// </param>
        /// <param name="options">Additional options, such as optimization and logging levels.</param>
        /// <param name="sourceFileProvider">Interface for source file access.</param>
        [NotNull]
        public static CompilationResult Compile(
            [NotNull] string mainModule,
            [NotNull] CompilationOptions options,
            [NotNull] ISourceFileProvider sourceFileProvider)
        {
            // TODO: Determinism (basically, compile everything in a fixed order)

            var compilation = new Compilation();

            // TODO: Build the dependency graph and decide the build order

            // Parse each module
            // TODO: Make this run in parallel
            ParseModule(mainModule, compilation, sourceFileProvider);

            // TODO: Compile modules once they and their dependencies are parsed (if there were no parsing errors)
            // This includes first adding type and method information to the compilation:
            //   - First add user-defined types
            //   - Then add methods with fully resolved parameter/return types
            // After this, the module should be ready for semantic compilation in any order of methods

            // TODO: Run the optimizer if enabled

            // TODO: Generate code

            // Return statistics
            // At this point, the compilation is no more accessed from multiple threads,
            // and it is safe to access diagnostics without locking.
            if (compilation.HasErrors)
            {
                return new CompilationResult(1, 0, 1, compilation.Diagnostics);
            }
            else
            {
                return new CompilationResult(1, 1, 0, compilation.Diagnostics);
            }
        }

        /// <summary>
        /// Internal for testing only.
        /// Parses a single module and adds the parsed syntax tree, diagnostics and type information to the compilation.
        /// </summary>
        internal static void ParseModule([NotNull] string moduleName, [NotNull] Compilation compilation,
            [NotNull] ISourceFileProvider fileProvider)
        {
            // Parse each source file
            if (!fileProvider.TryGetFilenamesForModule(moduleName, out var filesToParse))
            {
                compilation.AddMissingModuleError(moduleName);
                return;
            }
            Debug.Assert(filesToParse != null);

            var allDiagnostics = new List<Diagnostic>();

            foreach (var filename in filesToParse)
            {
                if (!fileProvider.TryGetSourceFile(filename, out var sourceBytes))
                {
                    compilation.AddMissingFileError(moduleName, filename);
                    continue;
                }
                
                // TODO: Add caching for diagnostic sinks
                var diagnosticSink = new SingleFileDiagnosticSink(moduleName, filename);

                var syntaxTree = SyntaxParser.Parse(sourceBytes, diagnosticSink);
                allDiagnostics.AddRange(diagnosticSink.Diagnostics);
            }

            // Log diagnostics for all the files at once
            compilation.AddDiagnostics(allDiagnostics);

            // TODO: Store the syntax trees somewhere for the semantic compiler to find (not Compilation?)
        }
    }
}

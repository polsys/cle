using System;
using System.Collections.Generic;
using System.Diagnostics;
using Cle.Common;
using Cle.Parser;
using Cle.Parser.SyntaxTree;
using Cle.SemanticAnalysis;
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
            ParseModule(mainModule, compilation, sourceFileProvider, out var syntaxTrees);

            // TODO: Compile modules only once they and their dependencies are parsed (if there were no parsing errors)
            // TODO: Make this parallel
            if (!compilation.HasErrors)
            {
                AddDeclarationsForModule(mainModule, compilation, syntaxTrees);
            }

            // After this, the module should be ready for semantic compilation in any order of methods
            if (!compilation.HasErrors)
            {
                CompileModule(mainModule, compilation, syntaxTrees);
            }

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
        /// Parses a single module and returns the successfully parsed syntax trees.
        /// Adds parsing diagnostics to the compilation.
        /// </summary>
        internal static void ParseModule([NotNull] string moduleName, [NotNull] Compilation compilation,
            [NotNull] ISourceFileProvider fileProvider, [NotNull, ItemNotNull] out List<SourceFileSyntax> syntaxTrees)
        {
            syntaxTrees = new List<SourceFileSyntax>();

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

                var syntaxTree = SyntaxParser.Parse(sourceBytes, filename, diagnosticSink);
                if (syntaxTree != null)
                {
                    syntaxTrees.Add(syntaxTree);
                }

                allDiagnostics.AddRange(diagnosticSink.Diagnostics);
            }

            // Log diagnostics for all the files at once
            compilation.AddDiagnostics(allDiagnostics);
        }

        private static void AddDeclarationsForModule(
            [NotNull] string moduleName,
            [NotNull] Compilation compilation,
            [NotNull, ItemNotNull] List<SourceFileSyntax> syntaxTrees)
        {
            // First, add type and method information to the compilation:
            //   - TODO: First add user-defined types
            //   - Then add methods with fully resolved parameter/return types
            foreach (var sourceFile in syntaxTrees)
            {
                foreach (var methodSyntax in sourceFile.Functions)
                {
                    // TODO: Caching of diagnostic sinks
                    var diagnosticSink = new SingleFileDiagnosticSink(moduleName, sourceFile.Filename);
                    var decl = MethodCompiler.CompileDeclaration(methodSyntax, sourceFile.Filename,
                        compilation.ReserveMethodSlot(), compilation, diagnosticSink);

                    if (decl != null)
                    {
                        // Declaration is valid, but we still have to verify that the name is not taken
                        if (!compilation.AddMethodDeclaration(methodSyntax.Name, sourceFile.Namespace, decl))
                        {
                            diagnosticSink.Add(DiagnosticCode.MethodAlreadyDefined, decl.DefinitionPosition,
                                sourceFile.Namespace + "::" + methodSyntax.Name);
                            compilation.AddDiagnostics(diagnosticSink.Diagnostics);
                        }
                    }
                    else
                    {
                        compilation.AddDiagnostics(diagnosticSink.Diagnostics);
                    }
                }
            }
        }

        private static void CompileModule(
            [NotNull] string moduleName,
            [NotNull] Compilation compilation,
            [NotNull, ItemNotNull] List<SourceFileSyntax> syntaxTrees)
        {
            // Compile each method body
            foreach (var sourceFile in syntaxTrees)
            {
                // Compilation.GetMethodDeclarations expects a list of visible namespaces -
                // here we have a single element only
                var sourceFileNamespaceAsArray = new[] { sourceFile.Namespace };

                foreach (var methodSyntax in sourceFile.Functions)
                {
                    // TODO: Caching of diagnostic sinks - this allows reusing the compiler instance
                    var diagnosticSink = new SingleFileDiagnosticSink(moduleName, sourceFile.Filename);
                    var compiler = new MethodCompiler(compilation, diagnosticSink);

                    // Get the method declaration
                    var possibleDeclarations = compilation.GetMethodDeclarations(methodSyntax.Name,
                        sourceFileNamespaceAsArray, sourceFile.Filename);
                    if (possibleDeclarations.Count != 1)
                        throw new InvalidOperationException("Ambiguous or missing method declaration");
                    var declaration = possibleDeclarations[0];

                    // Compile and store the method body
                    var methodBody = compiler.CompileBody(methodSyntax, declaration,
                        sourceFile.Namespace, sourceFile.Filename);
                    if (methodBody != null)
                    {
                        compilation.SetMethodBody(declaration.BodyIndex, methodBody);
                    }
                    compilation.AddDiagnostics(diagnosticSink.Diagnostics);
                }
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Cle.CodeGeneration;
using Cle.Common;
using Cle.Parser;
using Cle.Parser.SyntaxTree;
using Cle.SemanticAnalysis;

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
        /// <param name="options">Additional options, such as optimization and logging levels.</param>
        /// <param name="sourceFileProvider">Interface for source file access.</param>
        /// <param name="outputFileProvider">Interface for output file access.</param>
        public static CompilationResult Compile(
            CompilationOptions options,
            ISourceFileProvider sourceFileProvider,
            IOutputFileProvider outputFileProvider)
        {
            // TODO: Determinism (basically, compile everything in a fixed order)

            var compilation = new Compilation();

            // Create a debug logger. If logging is not enabled, all operations do nothing.
            var debugLogger = new DebugLogger(
                options.DebugOutput ? outputFileProvider.GetDebugFileWriter() : null,
                options.DebugPattern);

            // TODO: Build the dependency graph and decide the build order
            var mainModule = options.MainModule;

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
                debugLogger.WriteHeader("SEMANTIC COMPILATION PHASE");
                CompileModule(mainModule, compilation, syntaxTrees, debugLogger);
            }

            // There must be a main method
            if (!compilation.HasErrors && compilation.EntryPointIndex == -1)
            {
                compilation.AddModuleLevelDiagnostic(DiagnosticCode.NoEntryPointProvided, mainModule);
            }

            // TODO: Run the optimizer if enabled

            // Generate code
            if (!compilation.HasErrors)
            {
                var outputStream = outputFileProvider.GetExecutableStream();
                var disassemblyWriter = options.EmitDisassembly ? outputFileProvider.GetDisassemblyWriter() : null;
                if (outputStream is null || (options.EmitDisassembly && disassemblyWriter is null))
                {
                    // TODO: Also include the failed file name
                    compilation.AddModuleLevelDiagnostic(DiagnosticCode.CouldNotCreateOutputFile, mainModule);
                }
                else
                {
                    debugLogger.WriteHeader("CODE GENERATION PHASE");
                    GenerateCode(compilation, outputStream, disassemblyWriter, debugLogger);
                }
            }

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
        internal static void ParseModule(string moduleName, Compilation compilation,
            ISourceFileProvider fileProvider, out List<SourceFileSyntax> syntaxTrees)
        {
            syntaxTrees = new List<SourceFileSyntax>();

            // Parse each source file
            if (!fileProvider.TryGetFilenamesForModule(moduleName, out var filesToParse))
            {
                compilation.AddModuleLevelDiagnostic(DiagnosticCode.ModuleNotFound, moduleName);
                return;
            }
            Debug.Assert(filesToParse != null);

            var allDiagnostics = new List<Diagnostic>();
            var diagnosticSink = new SingleFileDiagnosticSink();

            foreach (var filename in filesToParse)
            {
                if (!fileProvider.TryGetSourceFile(filename, out var sourceBytes))
                {
                    compilation.AddMissingFileError(moduleName, filename);
                    continue;
                }

                diagnosticSink.Reset(moduleName, filename);
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
            string moduleName,
            Compilation compilation,
            List<SourceFileSyntax> syntaxTrees)
        {
            var diagnosticSink = new SingleFileDiagnosticSink();

            // First, add type and method information to the compilation:
            //   - TODO: First add user-defined types
            //   - Then add methods with fully resolved parameter/return types
            foreach (var sourceFile in syntaxTrees)
            {
                diagnosticSink.Reset(moduleName, sourceFile.Filename);

                foreach (var methodSyntax in sourceFile.Functions)
                {
                    var decl = MethodDeclarationCompiler.Compile(methodSyntax, sourceFile.Namespace, sourceFile.Filename,
                        compilation.ReserveMethodSlot(), compilation, diagnosticSink);

                    // Declaration is invalid
                    if (decl is null)
                        continue;

                    // Add the declaration to compilation, verifying that the name is not taken
                    var fullName = sourceFile.Namespace + "::" + methodSyntax.Name;
                    if (!compilation.AddMethodDeclaration(methodSyntax.Name, sourceFile.Namespace, decl))
                    {
                        diagnosticSink.Add(DiagnosticCode.MethodAlreadyDefined,
                            decl.DefinitionPosition, fullName);
                    }

                    // If the declaration has an [EntryPoint] attribute, store that
                    // TODO: Only do this when compiling the the main module
                    if (decl.IsEntryPoint)
                    {
                        if (!compilation.TrySetEntryPointIndex(decl.BodyIndex))
                        {
                            diagnosticSink.Add(DiagnosticCode.MultipleEntryPointsProvided,
                                decl.DefinitionPosition, fullName);
                        }
                    }
                }

                compilation.AddDiagnostics(diagnosticSink.Diagnostics);
            }
        }

        private static void CompileModule(
            string moduleName,
            Compilation compilation,
            List<SourceFileSyntax> syntaxTrees,
            DebugLogger debugLogger)
        {
            var diagnosticSink = new SingleFileDiagnosticSink();
            var compiler = new MethodCompiler(compilation, diagnosticSink);
            var ssaConverter = new SsaConverter();

            // Compile each method body
            foreach (var sourceFile in syntaxTrees)
            {
                // Compilation.GetMethodDeclarations expects a list of visible namespaces -
                // here we have a single element only
                var sourceFileNamespaceAsArray = new[] { sourceFile.Namespace };
                diagnosticSink.Reset(moduleName, sourceFile.Filename);

                foreach (var methodSyntax in sourceFile.Functions)
                {
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
                        // Convert the method to SSA and store the converted method
                        var ssaBody = ssaConverter.ConvertToSsa(methodBody);
                        compilation.SetMethodBody(declaration.BodyIndex, ssaBody);

                        if (debugLogger.ShouldLog(methodBody.FullName))
                        {
                            debugLogger.WriteLine("Before SSA conversion:");
                            debugLogger.DumpMethod(methodBody);
                            debugLogger.WriteLine("After SSA conversion:");
                            debugLogger.DumpMethod(ssaBody);
                        }
                    }
                }

                compilation.AddDiagnostics(diagnosticSink.Diagnostics);
            }
        }

        private static void GenerateCode(Compilation compilation, Stream outputStream,
            TextWriter? disassemblyWriter, DebugLogger debugLogger)
        {
            var generator = new WindowsX64CodeGenerator(outputStream, disassemblyWriter);
            
            // TODO: This acquires the method body lock once for each method - refactor Compilation
            var highestBodyIndex = compilation.MethodBodyCount;
            for (var i = 0; i < highestBodyIndex; i++)
            {
                var methodBody = compilation.GetMethodBody(i);
                var dumpWriterForMethod = debugLogger.ShouldLog(methodBody.FullName) ? debugLogger.Writer : null;

                generator.EmitMethod(methodBody, i, compilation.EntryPointIndex == i, dumpWriterForMethod);
            }

            generator.FinalizeFile();
        }
    }
}

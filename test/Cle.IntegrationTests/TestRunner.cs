using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Cle.Common;
using Cle.Compiler;
using Cle.Frontend;
using NUnit.Framework;

namespace Cle.IntegrationTests
{
    /// <summary>
    /// Fluent utility for compiling and running integration test cases.
    /// </summary>
    internal class TestRunner
    {
        private readonly string _absoluteModulePath;
        private bool _emitDisassembly;

        /// <summary>
        /// Creates a test runner for the specified module.
        /// </summary>
        /// <param name="modulePath">Path relative to the integration test assembly.</param>
        public TestRunner(string modulePath)
        {
            _absoluteModulePath = Path.GetFullPath(modulePath, 
                Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
        }

        public TestRunner WithDisassembly()
        {
            _emitDisassembly = true;
            return this;
        }

        /// <summary>
        /// Compiles the module with a new compiler instance created within the current process.
        /// </summary>
        public CompilationResult CompileInProcess()
        {
            // Delete output directories from previous compilation
            var binPath = Path.Combine(_absoluteModulePath, "_bin");
            if (Directory.Exists(binPath))
            {
                Directory.Delete(binPath, true);
            }

            // Run the compiler as the frontend would
            var sourceProvider = new SourceFileProvider(_absoluteModulePath);
            using (var outputProvider = new OutputFileProvider(_absoluteModulePath, "out.exe"))
            {
                var options = new CompilationOptions(".", emitDisassembly: _emitDisassembly);
                var result = CompilerDriver.Compile(options, sourceProvider, outputProvider);

                // TODO: Support optimized builds, other platforms
                var executablePath = Path.Combine(_absoluteModulePath, "_bin/windows-x64/out.exe");
                return new CompilationResult(result.Diagnostics, executablePath);
            }
        }
    }

    internal class CompilationResult
    {
        private readonly bool _succeeded;
        private readonly IReadOnlyList<Diagnostic> _diagnostics;
        private readonly string _executablePath;

        public CompilationResult(IReadOnlyList<Diagnostic> diagnostics, string executablePath)
        {
            _succeeded = true;
            foreach (var diagnostic in diagnostics)
            {
                if (diagnostic.IsError)
                    _succeeded = false;
            }

            _diagnostics = diagnostics;
            _executablePath = executablePath;
        }

        /// <summary>
        /// Asserts that there were no compilation errors.
        /// </summary>
        public CompilationResult VerifyCompilationSucceeded()
        {
            // This also displays the first diagnostic with IsError, if any
            Assert.That(_diagnostics, Has.All.Matches((Diagnostic diagnostic) => !diagnostic.IsError));
            return this;
        }

        /// <summary>
        /// Runs the produced executable.
        /// Throws if the compilation failed; use <see cref="VerifyCompilationSucceeded"/> for a more detailed message.
        /// </summary>
        public ExecutionResult Run(int timeoutMilliseconds)
        {
            Assert.That(_succeeded, Is.True, "Compilation did not succeed");
            Assert.That(_executablePath, Does.Exist);

            using (var process = new Process())
            {
                process.StartInfo.FileName = _executablePath;
                process.StartInfo.WorkingDirectory = Path.GetDirectoryName(_executablePath);
                
                Assert.That(process.Start(), Is.True);
                if (!process.WaitForExit(timeoutMilliseconds))
                {
                    process.Kill();
                    Assert.Fail("The process did not exit within the allocated time and was killed.");
                }

                return new ExecutionResult(process.ExitCode);
            }
        }
    }

    internal class ExecutionResult
    {
        private readonly int _returnCode;

        public ExecutionResult(int returnCode)
        {
            _returnCode = returnCode;
        }

        /// <summary>
        /// Asserts that the return code matches <paramref name="expected"/>.
        /// </summary>
        /// <param name="expected">The expected return code from the program.</param>
        public ExecutionResult VerifyReturnCode(int expected)
        {
            Assert.That(_returnCode, Is.EqualTo(expected));
            return this;
        }
    }
}

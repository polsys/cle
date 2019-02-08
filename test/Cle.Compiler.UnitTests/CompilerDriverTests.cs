using Cle.Common;
using NUnit.Framework;

namespace Cle.Compiler.UnitTests
{
    public class CompilerDriverTests
    {
        [Test]
        public void Compile_exits_on_parse_error()
        {
            const string source = @"namespace Test";
            var sourceProvider = new TestingSourceFileProvider();
            sourceProvider.Add(".", "main.cle", source);

            using (var outputProvider = new TestingOutputFileProvider())
            {
                var result = CompilerDriver.Compile(new CompilationOptions("."), sourceProvider, outputProvider);

                Assert.That(result.FailedCount, Is.EqualTo(1));
                Assert.That(result.ModuleCount, Is.EqualTo(1));
                Assert.That(result.SucceededCount, Is.EqualTo(0));

                Assert.That(result.Diagnostics, Has.Exactly(1).Items);
                Assert.That(result.Diagnostics[0].Code, Is.EqualTo(DiagnosticCode.ExpectedSemicolon));
                Assert.That(result.Diagnostics[0].Position.Line, Is.EqualTo(1));
                Assert.That(result.Diagnostics[0].Position.ByteInLine, Is.EqualTo(14));
                Assert.That(result.Diagnostics[0].Module, Is.EqualTo("."));
                Assert.That(result.Diagnostics[0].Filename, Is.EqualTo("main.cle"));
            }
        }

        [Test]
        public void Compile_exits_on_declaration_error()
        {
            const string source = @"namespace Test;

public NonexistentType Main() {}";
            var sourceProvider = new TestingSourceFileProvider();
            sourceProvider.Add(".", "main.cle", source);

            using (var outputProvider = new TestingOutputFileProvider())
            {
                var result = CompilerDriver.Compile(new CompilationOptions("."), sourceProvider, outputProvider);

                Assert.That(result.FailedCount, Is.EqualTo(1));
                Assert.That(result.ModuleCount, Is.EqualTo(1));
                Assert.That(result.SucceededCount, Is.EqualTo(0));

                Assert.That(result.Diagnostics, Has.Exactly(1).Items);
                Assert.That(result.Diagnostics[0].Code, Is.EqualTo(DiagnosticCode.TypeNotFound));
                Assert.That(result.Diagnostics[0].Actual, Is.EqualTo("NonexistentType"));
                Assert.That(result.Diagnostics[0].Position.Line, Is.EqualTo(3));
                Assert.That(result.Diagnostics[0].Position.ByteInLine, Is.EqualTo(0));
                Assert.That(result.Diagnostics[0].Module, Is.EqualTo("."));
                Assert.That(result.Diagnostics[0].Filename, Is.EqualTo("main.cle"));
            }
        }

        [Test]
        public void Compile_exits_on_semantic_error()
        {
            const string source = @"namespace Test;

public bool TypeMismatch() { return 2; }";
            var sourceProvider = new TestingSourceFileProvider();
            sourceProvider.Add(".", "main.cle", source);

            using (var outputProvider = new TestingOutputFileProvider())
            {
                var result = CompilerDriver.Compile(new CompilationOptions("."), sourceProvider, outputProvider);

                Assert.That(result.FailedCount, Is.EqualTo(1));
                Assert.That(result.ModuleCount, Is.EqualTo(1));
                Assert.That(result.SucceededCount, Is.EqualTo(0));

                Assert.That(result.Diagnostics, Has.Exactly(1).Items);
                Assert.That(result.Diagnostics[0].Code, Is.EqualTo(DiagnosticCode.TypeMismatch));
                Assert.That(result.Diagnostics[0].Actual, Is.EqualTo("int32"));
                Assert.That(result.Diagnostics[0].Expected, Is.EqualTo("bool"));
                Assert.That(result.Diagnostics[0].Position.Line, Is.EqualTo(3));
                Assert.That(result.Diagnostics[0].Position.ByteInLine, Is.EqualTo(36));
                Assert.That(result.Diagnostics[0].Module, Is.EqualTo("."));
                Assert.That(result.Diagnostics[0].Filename, Is.EqualTo("main.cle"));
            }
        }

        [Test]
        public void Compile_exits_on_multiple_declaration_error()
        {
            const string source1 = @"namespace Test;
public int32 IntFunction() {}";
            const string source2 = @"namespace Test;
internal int32 IntFunction() {}";
            var sourceProvider = new TestingSourceFileProvider();
            sourceProvider.Add(".", "file1.cle", source1);
            sourceProvider.Add(".", "file2.cle", source2);

            using (var outputProvider = new TestingOutputFileProvider())
            {
                var result = CompilerDriver.Compile(new CompilationOptions("."), sourceProvider, outputProvider);

                Assert.That(result.FailedCount, Is.EqualTo(1));
                Assert.That(result.ModuleCount, Is.EqualTo(1));
                Assert.That(result.SucceededCount, Is.EqualTo(0));

                Assert.That(result.Diagnostics, Has.Exactly(1).Items);
                Assert.That(result.Diagnostics[0].Code, Is.EqualTo(DiagnosticCode.MethodAlreadyDefined));
                Assert.That(result.Diagnostics[0].Actual, Is.EqualTo("Test::IntFunction"));
                Assert.That(result.Diagnostics[0].Position.Line, Is.EqualTo(2));
                Assert.That(result.Diagnostics[0].Position.ByteInLine, Is.EqualTo(0));
                Assert.That(result.Diagnostics[0].Module, Is.EqualTo("."));
                Assert.That(result.Diagnostics[0].Filename, Is.EqualTo("file2.cle"));
            }
        }
        
        [Test]
        public void Compile_exits_if_main_module_provides_no_entry_point()
        {
            const string source1 = @"namespace Test;";
            var sourceProvider = new TestingSourceFileProvider();
            sourceProvider.Add(".", "main.cle", source1);

            using (var outputProvider = new TestingOutputFileProvider())
            {
                var result = CompilerDriver.Compile(new CompilationOptions("."), sourceProvider, outputProvider);

                Assert.That(result.FailedCount, Is.EqualTo(1));
                Assert.That(result.ModuleCount, Is.EqualTo(1));
                Assert.That(result.SucceededCount, Is.EqualTo(0));

                Assert.That(result.Diagnostics, Has.Exactly(1).Items);
                Assert.That(result.Diagnostics[0].Code, Is.EqualTo(DiagnosticCode.NoEntryPointProvided));
                Assert.That(result.Diagnostics[0].Module, Is.EqualTo("."));
            }
        }

        // TODO: When the module system exists, a test should allow having entry points in separate modules
        [Test]
        public void Compile_exits_if_main_module_provides_multiple_entry_points()
        {
            const string source1 = @"namespace Test;
[EntryPoint]
private int32 Main() { return 0; }";
            const string source2 = @"namespace Test::BetterFunctions;
[EntryPoint]
private int32 MuchBetterMain() { return 42; }";
            var sourceProvider = new TestingSourceFileProvider();
            sourceProvider.Add(".", "file1.cle", source1);
            sourceProvider.Add(".", "file2.cle", source2);

            using (var outputProvider = new TestingOutputFileProvider())
            {
                var result = CompilerDriver.Compile(new CompilationOptions("."), sourceProvider, outputProvider);

                Assert.That(result.FailedCount, Is.EqualTo(1));
                Assert.That(result.ModuleCount, Is.EqualTo(1));
                Assert.That(result.SucceededCount, Is.EqualTo(0));

                Assert.That(result.Diagnostics, Has.Exactly(1).Items);
                Assert.That(result.Diagnostics[0].Code, Is.EqualTo(DiagnosticCode.MultipleEntryPointsProvided));
                Assert.That(result.Diagnostics[0].Actual, Is.EqualTo("Test::BetterFunctions::MuchBetterMain"));
                Assert.That(result.Diagnostics[0].Position.Line, Is.EqualTo(3));
                Assert.That(result.Diagnostics[0].Module, Is.EqualTo("."));
                Assert.That(result.Diagnostics[0].Filename, Is.EqualTo("file2.cle"));
            }
        }
        
        [TestCase(true, false)]
        [TestCase(false, true)]
        public void Compile_fails_if_output_file_cannot_be_created(bool failExecutable, bool failDisassembly)
        {
            const string source = @"namespace Test;
[EntryPoint]
private int32 Main() { return 0; }";
            var sourceProvider = new TestingSourceFileProvider();
            sourceProvider.Add(".", "main.cle", source);

            using (var outputProvider = new TestingOutputFileProvider())
            {
                outputProvider.FailExecutable = failExecutable;
                outputProvider.FailDisassembly = failDisassembly;
                var options = new CompilationOptions(".", emitDisassembly: true);
                var result = CompilerDriver.Compile(options, sourceProvider, outputProvider);

                Assert.That(result.FailedCount, Is.EqualTo(1));
                Assert.That(result.ModuleCount, Is.EqualTo(1));
                Assert.That(result.SucceededCount, Is.EqualTo(0));

                Assert.That(result.Diagnostics, Has.Exactly(1).Items);
                Assert.That(result.Diagnostics[0].Code, Is.EqualTo(DiagnosticCode.CouldNotCreateOutputFile));
                Assert.That(result.Diagnostics[0].Position.Line, Is.EqualTo(0));
                Assert.That(result.Diagnostics[0].Module, Is.EqualTo("."));
                Assert.That(result.Diagnostics[0].Filename, Is.Null); // TODO: Remove this limitation
            }
        }
        
        [TestCase(true)]
        [TestCase(false)]
        public void Compile_produces_output_successfully(bool emitDisassembly)
        {
            const string source = @"namespace Test;
[EntryPoint]
private int32 Main() { return 0; }";
            var sourceProvider = new TestingSourceFileProvider();
            sourceProvider.Add(".", "main.cle", source);

            using (var outputProvider = new TestingOutputFileProvider())
            {
                var options = new CompilationOptions(".", emitDisassembly: emitDisassembly);
                var result = CompilerDriver.Compile(options, sourceProvider, outputProvider);

                Assert.That(result.FailedCount, Is.EqualTo(0));
                Assert.That(result.ModuleCount, Is.EqualTo(1));
                Assert.That(result.SucceededCount, Is.EqualTo(1));
                Assert.That(result.Diagnostics, Has.Exactly(0).Items);

                Assert.That(outputProvider.ExecutableStream, Is.Not.Null);
                Assert.That(outputProvider.ExecutableStream.Position, Is.GreaterThan(0));

                if (emitDisassembly)
                {
                    Assert.That(outputProvider.DisassemblyWriter, Is.Not.Null);
                    Assert.That(outputProvider.DisassemblyWriter.ToString(), Does.Contain("Test::Main"));
                }
                else
                {
                    Assert.That(outputProvider.DisassemblyWriter, Is.Null);
                }
            }
        }

        [Test]
        public void ParseModule_parses_single_file_successfully()
        {
            const string source = @"namespace Test;
private int32 PrivateFunc() {}
internal bool ProtectedFunc() {}
public void PublicFunc() {}";
            var sourceProvider = new TestingSourceFileProvider();
            sourceProvider.Add(".", "main.cle", source);

            var compilation = new Compilation();
            CompilerDriver.ParseModule(".", compilation, sourceProvider, out var syntaxTrees);

            sourceProvider.AssertFileWasRead("main.cle");
            Assert.That(compilation.Diagnostics, Is.Empty);
            Assert.That(syntaxTrees, Has.Exactly(1).Items);
        }

        [Test]
        public void ParseModule_parses_two_files_successfully()
        {
            const string source1 = @"namespace Test;
private int32 PrivateFunc() {}
internal bool ProtectedFunc() {}
public void PublicFunc() {}";
            const string source2 = @"namespace Test;
public int32 OneMoreFunc() {}";

            var sourceProvider = new TestingSourceFileProvider();
            sourceProvider.Add(".", "main.cle", source1);
            sourceProvider.Add(".", "other.cle", source2);

            var compilation = new Compilation();
            CompilerDriver.ParseModule(".", compilation, sourceProvider, out var syntaxTrees);

            sourceProvider.AssertFileWasRead("main.cle");
            sourceProvider.AssertFileWasRead("other.cle");
            Assert.That(compilation.Diagnostics, Is.Empty);
            Assert.That(syntaxTrees, Has.Exactly(2).Items);
        }

        [Test]
        public void ParseModule_parses_two_files_with_errors_in_both()
        {
            const string source1 = @"namespace Test";
            const string source2 = @"namespace Test;
public int32 Func {}";

            var sourceProvider = new TestingSourceFileProvider();
            sourceProvider.Add(".", "main.cle", source1);
            sourceProvider.Add(".", "other.cle", source2);

            var compilation = new Compilation();
            CompilerDriver.ParseModule(".", compilation, sourceProvider, out var syntaxTrees);

            sourceProvider.AssertFileWasRead("main.cle");
            sourceProvider.AssertFileWasRead("other.cle");
            Assert.That(compilation.Diagnostics, Has.Exactly(2).Items);
            Assert.That(syntaxTrees, Is.Empty);

            Assert.That(compilation.Diagnostics[0].Module, Is.EqualTo("."));
            Assert.That(compilation.Diagnostics[0].Filename, Is.EqualTo("main.cle"));
            Assert.That(compilation.Diagnostics[0].Code, Is.EqualTo(DiagnosticCode.ExpectedSemicolon));

            Assert.That(compilation.Diagnostics[1].Module, Is.EqualTo("."));
            Assert.That(compilation.Diagnostics[1].Filename, Is.EqualTo("other.cle"));
            Assert.That(compilation.Diagnostics[1].Code, Is.EqualTo(DiagnosticCode.ExpectedParameterList));
        }

        [Test]
        public void ParseModule_raises_error_on_unavailable_source_file()
        {
            var sourceProvider = new TestingSourceFileProvider();
            sourceProvider.Add(".", "main.cle", null);

            var compilation = new Compilation();
            CompilerDriver.ParseModule(".", compilation, sourceProvider, out var syntaxTrees);
            
            Assert.That(compilation.Diagnostics, Has.Exactly(1).Items);
            Assert.That(compilation.Diagnostics[0].Code, Is.EqualTo(DiagnosticCode.SourceFileNotFound));
            Assert.That(compilation.Diagnostics[0].Filename, Is.EqualTo("main.cle"));
            Assert.That(syntaxTrees, Is.Empty);
        }

        [Test]
        public void ParseModule_raises_error_on_unavailable_module()
        {
            var sourceProvider = new TestingSourceFileProvider();
            sourceProvider.Add(".", "main.cle", "");

            var compilation = new Compilation();
            CompilerDriver.ParseModule("OtherModule", compilation, sourceProvider, out var syntaxTrees);
            
            Assert.That(compilation.Diagnostics, Has.Exactly(1).Items);
            Assert.That(compilation.Diagnostics[0].Code, Is.EqualTo(DiagnosticCode.ModuleNotFound));
            Assert.That(compilation.Diagnostics[0].Module, Is.EqualTo("OtherModule"));
            Assert.That(syntaxTrees, Is.Empty);
        }

        [Test]
        public void Debug_logging_is_written()
        {
            const string source = @"namespace Test;
public bool SimpleMethod() { return true; }
public int32 ComplexMethod() { return 42; }";
            var sourceProvider = new TestingSourceFileProvider();
            sourceProvider.Add(".", "main.cle", source);

            using (var outputProvider = new TestingOutputFileProvider())
            {
                var _ = CompilerDriver.Compile(new CompilationOptions(".", debugPattern: "Simple"),
                    sourceProvider, outputProvider);

                var writer = outputProvider.DebugWriter;
                Assert.That(writer.ToString(), Does.Contain("; Test::SimpleMethod"));
                Assert.That(writer.ToString(), Does.Contain("  Return #0"));
                Assert.That(writer.ToString(), Does.Not.Contain("ComplexMethod"));
            }
        }

        [Test]
        public void Debug_log_is_not_created_if_logging_is_disabled()
        {
            const string source = @"namespace Test;
public bool SimpleMethod() { return true; }
public int32 ComplexMethod() { return 42; }";
            var sourceProvider = new TestingSourceFileProvider();
            sourceProvider.Add(".", "main.cle", source);

            using (var outputProvider = new TestingOutputFileProvider())
            {
                var _ = CompilerDriver.Compile(new CompilationOptions("."),
                    sourceProvider, outputProvider);

                Assert.That(outputProvider.DebugWriter, Is.Null);
            }
        }
    }
}

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
            var fileProvider = new TestingSourceFileProvider();
            fileProvider.Add(".", "main.cle", source);

            var result = CompilerDriver.Compile(".", new CompilationOptions(), fileProvider);

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

        [Test]
        public void Compile_exits_on_declaration_error()
        {
            const string source = @"namespace Test;

public NonexistentType Main() {}";
            var fileProvider = new TestingSourceFileProvider();
            fileProvider.Add(".", "main.cle", source);

            var result = CompilerDriver.Compile(".", new CompilationOptions(), fileProvider);

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

        [Test]
        public void Compile_exits_on_multiple_declaration_error()
        {
            const string source1 = @"namespace Test;
public int32 IntFunction() {}";
            const string source2 = @"namespace Test;
internal int32 IntFunction() {}";
            var fileProvider = new TestingSourceFileProvider();
            fileProvider.Add(".", "file1.cle", source1);
            fileProvider.Add(".", "file2.cle", source2);

            var result = CompilerDriver.Compile(".", new CompilationOptions(), fileProvider);

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

        [Test]
        public void ParseModule_parses_single_file_successfully()
        {
            const string source = @"namespace Test;
private int32 PrivateFunc() {}
internal bool ProtectedFunc() {}
public void PublicFunc() {}";
            var fileProvider = new TestingSourceFileProvider();
            fileProvider.Add(".", "main.cle", source);

            var compilation = new Compilation();
            CompilerDriver.ParseModule(".", compilation, fileProvider, out var syntaxTrees);

            fileProvider.AssertFileWasRead("main.cle");
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

            var fileProvider = new TestingSourceFileProvider();
            fileProvider.Add(".", "main.cle", source1);
            fileProvider.Add(".", "other.cle", source2);

            var compilation = new Compilation();
            CompilerDriver.ParseModule(".", compilation, fileProvider, out var syntaxTrees);

            fileProvider.AssertFileWasRead("main.cle");
            fileProvider.AssertFileWasRead("other.cle");
            Assert.That(compilation.Diagnostics, Is.Empty);
            Assert.That(syntaxTrees, Has.Exactly(2).Items);
        }

        [Test]
        public void ParseModule_parses_two_files_with_errors_in_both()
        {
            const string source1 = @"namespace Test";
            const string source2 = @"namespace Test;
public int32 Func {}";

            var fileProvider = new TestingSourceFileProvider();
            fileProvider.Add(".", "main.cle", source1);
            fileProvider.Add(".", "other.cle", source2);

            var compilation = new Compilation();
            CompilerDriver.ParseModule(".", compilation, fileProvider, out var syntaxTrees);

            fileProvider.AssertFileWasRead("main.cle");
            fileProvider.AssertFileWasRead("other.cle");
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
            var fileProvider = new TestingSourceFileProvider();
            fileProvider.Add(".", "main.cle", null);

            var compilation = new Compilation();
            CompilerDriver.ParseModule(".", compilation, fileProvider, out var syntaxTrees);
            
            Assert.That(compilation.Diagnostics, Has.Exactly(1).Items);
            Assert.That(compilation.Diagnostics[0].Code, Is.EqualTo(DiagnosticCode.SourceFileNotFound));
            Assert.That(compilation.Diagnostics[0].Filename, Is.EqualTo("main.cle"));
            Assert.That(syntaxTrees, Is.Empty);
        }

        [Test]
        public void ParseModule_raises_error_on_unavailable_module()
        {
            var fileProvider = new TestingSourceFileProvider();
            fileProvider.Add(".", "main.cle", "");

            var compilation = new Compilation();
            CompilerDriver.ParseModule("OtherModule", compilation, fileProvider, out var syntaxTrees);
            
            Assert.That(compilation.Diagnostics, Has.Exactly(1).Items);
            Assert.That(compilation.Diagnostics[0].Code, Is.EqualTo(DiagnosticCode.ModuleNotFound));
            Assert.That(compilation.Diagnostics[0].Module, Is.EqualTo("OtherModule"));
            Assert.That(syntaxTrees, Is.Empty);
        }
    }
}

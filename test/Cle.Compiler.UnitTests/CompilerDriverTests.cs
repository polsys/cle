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
        public void ParseModule_parses_single_file_successfully()
        {
            const string source = @"namespace Test;
private int PrivateFunc() {}
internal bool ProtectedFunc() {}
public void PublicFunc() {}";
            var fileProvider = new TestingSourceFileProvider();
            fileProvider.Add(".", "main.cle", source);

            var compilation = new Compilation();
            CompilerDriver.ParseModule(".", compilation, fileProvider);

            fileProvider.AssertFileWasRead("main.cle");
            Assert.That(compilation.Diagnostics, Is.Empty);
            // TODO: Assert that the methods were added to the compilation
        }

        [Test]
        public void ParseModule_parses_two_files_successfully()
        {
            const string source1 = @"namespace Test;
private int PrivateFunc() {}
internal bool ProtectedFunc() {}
public void PublicFunc() {}";
            const string source2 = @"namespace Test;
public int OneMoreFunc() {}";

            var fileProvider = new TestingSourceFileProvider();
            fileProvider.Add(".", "main.cle", source1);
            fileProvider.Add(".", "other.cle", source2);

            var compilation = new Compilation();
            CompilerDriver.ParseModule(".", compilation, fileProvider);

            fileProvider.AssertFileWasRead("main.cle");
            fileProvider.AssertFileWasRead("other.cle");
            Assert.That(compilation.Diagnostics, Is.Empty);
            // TODO: Assert that the methods were added to the compilation
        }

        [Test]
        public void ParseModule_parses_two_files_with_errors_in_both()
        {
            const string source1 = @"namespace Test";
            const string source2 = @"namespace Test;
public int Func {}";

            var fileProvider = new TestingSourceFileProvider();
            fileProvider.Add(".", "main.cle", source1);
            fileProvider.Add(".", "other.cle", source2);

            var compilation = new Compilation();
            CompilerDriver.ParseModule(".", compilation, fileProvider);

            fileProvider.AssertFileWasRead("main.cle");
            fileProvider.AssertFileWasRead("other.cle");
            Assert.That(compilation.Diagnostics, Has.Exactly(2).Items);

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
            CompilerDriver.ParseModule(".", compilation, fileProvider);
            
            Assert.That(compilation.Diagnostics, Has.Exactly(1).Items);
            Assert.That(compilation.Diagnostics[0].Code, Is.EqualTo(DiagnosticCode.SourceFileNotFound));
            Assert.That(compilation.Diagnostics[0].Filename, Is.EqualTo("main.cle"));
        }

        [Test]
        public void ParseModule_raises_error_on_unavailable_module()
        {
            var fileProvider = new TestingSourceFileProvider();
            fileProvider.Add(".", "main.cle", "");

            var compilation = new Compilation();
            CompilerDriver.ParseModule("OtherModule", compilation, fileProvider);
            
            Assert.That(compilation.Diagnostics, Has.Exactly(1).Items);
            Assert.That(compilation.Diagnostics[0].Code, Is.EqualTo(DiagnosticCode.ModuleNotFound));
            Assert.That(compilation.Diagnostics[0].Module, Is.EqualTo("OtherModule"));
        }
    }
}

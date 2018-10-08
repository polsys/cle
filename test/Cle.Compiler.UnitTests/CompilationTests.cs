using Cle.Common;
using NUnit.Framework;

namespace Cle.Compiler.UnitTests
{
    public class CompilationTests
    {
        [Test]
        public void AddDiagnostics_with_empty_list()
        {
            var compilation = new Compilation();

            compilation.AddDiagnostics(new Diagnostic[] { });

            Assert.That(compilation.HasErrors, Is.False);
            Assert.That(compilation.Diagnostics, Is.Empty);
        }

        [Test]
        public void AddDiagnostics_with_multiple_errors()
        {
            var compilation = new Compilation();

            compilation.AddDiagnostics(new[]
            {
                new Diagnostic(DiagnosticCode.ExpectedSemicolon, default, "", "", null),
                new Diagnostic(DiagnosticCode.ExpectedMethodBody, default, "", "", null)
            });

            Assert.That(compilation.HasErrors, Is.True);
            Assert.That(compilation.Diagnostics, Has.Exactly(2).Items);
        }

        [Test]
        public void AddDiagnostics_with_warning()
        {
            var compilation = new Compilation();

            // TODO: This could be changed once real warnings exist in the codebase
            compilation.AddDiagnostics(new[]
                { new Diagnostic(DiagnosticCode.SemanticWarningStart, default, "", "", null) });

            Assert.That(compilation.HasErrors, Is.False);
            Assert.That(compilation.Diagnostics, Has.Exactly(1).Items);
        }

        [Test]
        public void AddMissingFileError_adds_error()
        {
            var compilation = new Compilation();

            compilation.AddMissingFileError("ModuleName", "FileName.cle");

            Assert.That(compilation.HasErrors, Is.True);
            Assert.That(compilation.Diagnostics, Has.Exactly(1).Items);
            Assert.That(compilation.Diagnostics[0].Module, Is.EqualTo("ModuleName"));
            Assert.That(compilation.Diagnostics[0].Filename, Is.EqualTo("FileName.cle"));
        }

        [Test]
        public void AddMissingModuleError_adds_error()
        {
            var compilation = new Compilation();

            compilation.AddMissingModuleError("ModuleName");

            Assert.That(compilation.HasErrors, Is.True);
            Assert.That(compilation.Diagnostics, Has.Exactly(1).Items);
            Assert.That(compilation.Diagnostics[0].Module, Is.EqualTo("ModuleName"));
            Assert.That(compilation.Diagnostics[0].Filename, Is.Null);
        }
    }
}

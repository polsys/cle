using Cle.Common;
using NUnit.Framework;

namespace Cle.SemanticAnalysis.UnitTests.MethodCompilerTests
{
    public class ReturnTests : MethodCompilerTestBase
    {
        [Test]
        public void Bool_constant_returning_method_compiled_successfully()
        {
            const string source = @"namespace Test;
public bool ReturnTrue() { return true; }";
            var compiledMethod = TryCompileSingleMethod(source, out var diagnostics);

            Assert.That(compiledMethod, Is.Not.Null);
            Assert.That(diagnostics.Diagnostics, Is.Empty);
            
            AssertDisassembly(compiledMethod, @"
; #0   bool = true
BB_0:
    Return #0");
        }

        [Test]
        public void Int32_constant_returning_method_compiled_successfully()
        {
            const string source = @"namespace Test;
public int32 GetTheAnswer() { return 42; }";
            var compiledMethod = TryCompileSingleMethod(source, out var diagnostics);

            Assert.That(compiledMethod, Is.Not.Null);
            Assert.That(diagnostics.Diagnostics, Is.Empty);
            
            AssertDisassembly(compiledMethod, @"
; #0   int32 = 42
BB_0:
    Return #0");
        }

        [Test]
        public void Explicit_void_return()
        {
            const string source = @"namespace Test;
public void DoNothing() { return; }";
            var compiledMethod = TryCompileSingleMethod(source, out var diagnostics);

            Assert.That(compiledMethod, Is.Not.Null);
            Assert.That(diagnostics.Diagnostics, Is.Empty);
            
            AssertDisassembly(compiledMethod, @"
; #0   void = void
BB_0:
    Return #0");
        }

        [Test]
        public void Return_type_is_validated()
        {
            const string source = @"namespace Test;
public bool Mismatch() { return 42; }";
            var compiledMethod = TryCompileSingleMethod(source, out var diagnostics);

            Assert.That(compiledMethod, Is.Null);
            diagnostics.AssertDiagnosticAt(DiagnosticCode.TypeMismatch, 2, 32)
                .WithActual("int32")
                .WithExpected("bool");
        }
        
        // TODO: Return guarantee and dead code warning (in separate test class)
        // TODO: Int32 expression return
        // TODO: Integer that does not fit in int32
    }
}

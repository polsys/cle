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
            var compiledMethod = TryCompileFirstMethod(source, out var diagnostics);

            Assert.That(compiledMethod, Is.Not.Null);
            Assert.That(diagnostics.Diagnostics, Is.Empty);
            
            AssertDisassembly(compiledMethod!, @"
; #0   bool
BB_0:
    Load true -> #0
    Return #0");
        }

        [Test]
        public void Int32_constant_returning_method_compiled_successfully()
        {
            const string source = @"namespace Test;
public int32 GetTheAnswer() { return 42; }";
            var compiledMethod = TryCompileFirstMethod(source, out var diagnostics);

            Assert.That(compiledMethod, Is.Not.Null);
            Assert.That(diagnostics.Diagnostics, Is.Empty);
            
            AssertDisassembly(compiledMethod!, @"
; #0   int32
BB_0:
    Load 42 -> #0
    Return #0");
        }

        [Test]
        public void Int32_constant_expression_returning_method_compiled_successfully()
        {
            const string source = @"namespace Test;
public int32 GetTheAnswer() { return 40 + 2; }";
            var compiledMethod = TryCompileFirstMethod(source, out var diagnostics);

            Assert.That(compiledMethod, Is.Not.Null);
            Assert.That(diagnostics.Diagnostics, Is.Empty);
            
            AssertDisassembly(compiledMethod!, @"
; #0   int32
BB_0:
    Load 42 -> #0
    Return #0");
        }

        [Test]
        public void Int32_expression_returning_method_compiled_successfully()
        {
            const string source = @"namespace Test;
public int32 GetTheAnswer() { var int32 almost = 40; return almost + 2; }";
            var compiledMethod = TryCompileFirstMethod(source, out var diagnostics);

            Assert.That(compiledMethod, Is.Not.Null);
            Assert.That(diagnostics.Diagnostics, Is.Empty);
            
            AssertDisassembly(compiledMethod!, @"
; #0   int32
; #1   int32
; #2   int32
BB_0:
    Load 40 -> #0
    Load 2 -> #1
    Add #0 + #1 -> #2
    Return #2");
        }

        [Test]
        public void Explicit_void_return()
        {
            const string source = @"namespace Test;
public void DoNothing() { return; }";
            var compiledMethod = TryCompileFirstMethod(source, out var diagnostics);

            Assert.That(compiledMethod, Is.Not.Null);
            Assert.That(diagnostics.Diagnostics, Is.Empty);
            
            AssertDisassembly(compiledMethod!, @"
; #0   void
BB_0:
    Return #0");
        }

        [Test]
        public void Implicit_void_return()
        {
            const string source = @"namespace Test;
public void DoNothing() { }";
            var compiledMethod = TryCompileFirstMethod(source, out var diagnostics);

            Assert.That(compiledMethod, Is.Not.Null);
            Assert.That(diagnostics.Diagnostics, Is.Empty);
            
            AssertDisassembly(compiledMethod!, @"
; #0   void
BB_0:
    Return #0");
        }

        [Test]
        public void Implicit_void_return_in_more_complex_method()
        {
            const string source = @"namespace Test;
public void DoNothing() { if (true) { return; } }";
            var compiledMethod = TryCompileFirstMethod(source, out var diagnostics);

            Assert.That(compiledMethod, Is.Not.Null);
            Assert.That(diagnostics.Diagnostics, Is.Empty);
            
            AssertDisassembly(compiledMethod!, @"
; #0   bool
; #1   void
; #2   void
BB_0:
    Load true -> #0
    BranchIf #0 ==> BB_1
    ==> BB_2

BB_1:
    Return #1

BB_2:
    Return #2");
        }

        [Test]
        public void Return_type_is_validated()
        {
            const string source = @"namespace Test;
public bool Mismatch() { return 42; }";
            var compiledMethod = TryCompileFirstMethod(source, out var diagnostics);

            Assert.That(compiledMethod, Is.Null);
            diagnostics.AssertDiagnosticAt(DiagnosticCode.TypeMismatch, 2, 32)
                .WithActual("int32")
                .WithExpected("bool");
        }

        [Test]
        public void Return_type_is_validated_in_void_return_within_non_void()
        {
            const string source = @"namespace Test;
public bool Mismatch() { return; }";
            var compiledMethod = TryCompileFirstMethod(source, out var diagnostics);

            Assert.That(compiledMethod, Is.Null);
            diagnostics.AssertDiagnosticAt(DiagnosticCode.TypeMismatch, 2, 25)
                .WithActual("void")
                .WithExpected("bool");
        }

        [Test]
        public void Return_type_is_validated_in_non_void_return_within_void()
        {
            const string source = @"namespace Test;
public void Mismatch() { return true; }";
            var compiledMethod = TryCompileFirstMethod(source, out var diagnostics);

            Assert.That(compiledMethod, Is.Null);
            diagnostics.AssertDiagnosticAt(DiagnosticCode.TypeMismatch, 2, 32)
                .WithActual("bool")
                .WithExpected("void");
        }
    }
}

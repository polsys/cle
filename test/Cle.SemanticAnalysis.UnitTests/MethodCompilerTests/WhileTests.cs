using Cle.Common;
using NUnit.Framework;

namespace Cle.SemanticAnalysis.UnitTests.MethodCompilerTests
{
    public class WhileTests : MethodCompilerTestBase
    {
        [Test]
        public void Empty_loop_with_constant_condition()
        {
            const string source = @"namespace Test;
public bool Stupid() {
    while (true) {
    }
    return false;
}";
            var compiledMethod = TryCompileFirstMethod(source, out var diagnostics);

            Assert.That(compiledMethod, Is.Not.Null);
            Assert.That(diagnostics.Diagnostics, Is.Empty);
            
            AssertDisassembly(compiledMethod, @"
; #0   bool = true
; #1   bool = false
BB_0:

BB_1:
    BranchIf #0 ==> BB_2
    ==> BB_3

BB_2:
    ==> BB_1

BB_3:
    Return #1");
        }

        [Test]
        public void More_complex_loop_with_constant_condition()
        {
            const string source = @"namespace Test;
public bool Stupid() {
    bool a = false;
    bool b = a;
    while (false) {
        if (a) { b = a; }
    }
    return b;
}";
            var compiledMethod = TryCompileFirstMethod(source, out var diagnostics);

            Assert.That(compiledMethod, Is.Not.Null);
            Assert.That(diagnostics.Diagnostics, Is.Empty);
            
            AssertDisassembly(compiledMethod, @"
; #0   bool = false
; #1   bool = void
; #2   bool = false
BB_0:
    CopyValue #0 -> #1

BB_1:
    BranchIf #2 ==> BB_2
    ==> BB_5

BB_2:
    BranchIf #0 ==> BB_3
    ==> BB_4

BB_3:
    CopyValue #0 -> #1

BB_4:
    ==> BB_1

BB_5:
    Return #1");
        }

        [Test]
        public void Condition_must_be_bool()
        {
            const string source = @"namespace Test;
public bool TypeMismatchInWhile() {
    while (42) {}
}";
            var compiledMethod = TryCompileFirstMethod(source, out var diagnostics);

            Assert.That(compiledMethod, Is.Null);
            diagnostics.AssertDiagnosticAt(DiagnosticCode.TypeMismatch, 3, 11).WithExpected("bool").WithActual("int32");
        }

        [Test]
        public void Body_must_be_valid()
        {
            const string source = @"namespace Test;
public bool TypeMismatchInWhile() {
    while (true) { bool a = 42; }
}";
            var compiledMethod = TryCompileFirstMethod(source, out var diagnostics);

            Assert.That(compiledMethod, Is.Null);
            diagnostics.AssertDiagnosticAt(DiagnosticCode.TypeMismatch, 3, 28).WithExpected("bool").WithActual("int32");
        }
    }
}

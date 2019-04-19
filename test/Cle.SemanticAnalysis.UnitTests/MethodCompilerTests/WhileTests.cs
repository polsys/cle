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
; #0   bool
; #1   bool
BB_0:

BB_1:
    Load true -> #0
    BranchIf #0 ==> BB_2
    ==> BB_3

BB_2:
    ==> BB_1

BB_3:
    Load false -> #1
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
            
            // The empty BB_4 must exist because otherwise the BB_2 --> BB_5 edge would be critical:
            // BB_2 has two successors and BB_5 has two predecessors (BB_3, and BB_2 via BB_4).
            AssertDisassembly(compiledMethod, @"
; #0   bool
; #1   bool
; #2   bool
BB_0:
    Load false -> #0
    CopyValue #0 -> #1

BB_1:
    Load false -> #2
    BranchIf #2 ==> BB_2
    ==> BB_6

BB_2:
    BranchIf #0 ==> BB_3
    ==> BB_4

BB_3:
    CopyValue #0 -> #1
    ==> BB_5

BB_4:

BB_5:
    ==> BB_1

BB_6:
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

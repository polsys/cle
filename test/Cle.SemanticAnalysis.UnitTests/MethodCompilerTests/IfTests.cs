using Cle.Common;
using NUnit.Framework;

namespace Cle.SemanticAnalysis.UnitTests.MethodCompilerTests
{
    public class IfTests : MethodCompilerTestBase
    {
        [Test]
        public void If_and_else_both_returning()
        {
            const string source = @"namespace Test;
public bool Contradiction() {
    if (true) {
        return false;
    } else {
        return true;
    }
}";
            var compiledMethod = TryCompileFirstMethod(source, out var diagnostics);

            Assert.That(compiledMethod, Is.Not.Null);
            Assert.That(diagnostics.Diagnostics, Is.Empty);
            
            AssertDisassembly(compiledMethod!, @"
; #0   bool
; #1   bool
; #2   bool
BB_0:
    Load true -> #0
    BranchIf #0 ==> BB_1
    ==> BB_2

BB_1:
    Load false -> #1
    Return #1

BB_2:
    Load true -> #2
    Return #2");
        }

        [Test]
        public void If_only_returning()
        {
            const string source = @"namespace Test;
public bool Tautology() {
    if (true) {
        return true;
    }
    return false;
}";
            var compiledMethod = TryCompileFirstMethod(source, out var diagnostics);

            Assert.That(compiledMethod, Is.Not.Null);
            Assert.That(diagnostics.Diagnostics, Is.Empty);
            
            AssertDisassembly(compiledMethod!, @"
; #0   bool
; #1   bool
; #2   bool
BB_0:
    Load true -> #0
    BranchIf #0 ==> BB_1
    ==> BB_2

BB_1:
    Load true -> #1
    Return #1

BB_2:
    Load false -> #2
    Return #2");
        }

        [Test]
        public void If_with_complex_condition()
        {
            const string source = @"namespace Test;
public bool Comparison() {
    int32 a = 42;
    if (a < 100) {
        return true;
    }
    return false;
}";
            var compiledMethod = TryCompileFirstMethod(source, out var diagnostics);

            Assert.That(compiledMethod, Is.Not.Null);
            Assert.That(diagnostics.Diagnostics, Is.Empty);
            
            AssertDisassembly(compiledMethod!, @"
; #0   int32
; #1   int32
; #2   bool
; #3   bool
; #4   bool
BB_0:
    Load 42 -> #0
    Load 100 -> #1
    Less #0 < #1 -> #2
    BranchIf #2 ==> BB_1
    ==> BB_2

BB_1:
    Load true -> #3
    Return #3

BB_2:
    Load false -> #4
    Return #4");
        }

        [Test]
        public void If_assigns_to_variable()
        {
            const string source = @"namespace Test;
public int32 TheAnswer() {
    int32 result = 42;
    if (false) {
        result = 1;
    }
    return result;
}";
            var compiledMethod = TryCompileFirstMethod(source, out var diagnostics);

            Assert.That(compiledMethod, Is.Not.Null);
            Assert.That(diagnostics.Diagnostics, Is.Empty);

            // BB_2 exists because direct BB_0 --> BB_3 would be a critical edge: BB_0 has two successors
            // and BB_3 has two predecessors. This causes issues for SSA because the PHI cannot be resolved.
            AssertDisassembly(compiledMethod!, @"
; #0   int32
; #1   bool
; #2   int32
BB_0:
    Load 42 -> #0
    Load false -> #1
    BranchIf #1 ==> BB_1
    ==> BB_2

BB_1:
    Load 1 -> #2
    CopyValue #2 -> #0
    ==> BB_3

BB_2:

BB_3:
    Return #0");
        }

        [Test]
        public void If_and_else_both_assigning_to_variable()
        {
            const string source = @"namespace Test;
public int32 ComplexAnswer() {
    int32 result = 0;
    if (true) {
        result = 42;
    } else {
        result = 41;
    }
    return result;
}";
            var compiledMethod = TryCompileFirstMethod(source, out var diagnostics);

            Assert.That(compiledMethod, Is.Not.Null);
            Assert.That(diagnostics.Diagnostics, Is.Empty);

            // Here are no critical edges as BB_1 and BB_2, the predecessors of BB_3, have only a single
            // successor each, and no other block has two predecessors.
            AssertDisassembly(compiledMethod!, @"
; #0   int32
; #1   bool
; #2   int32
; #3   int32
BB_0:
    Load 0 -> #0
    Load true -> #1
    BranchIf #1 ==> BB_1
    ==> BB_2

BB_1:
    Load 42 -> #2
    CopyValue #2 -> #0
    ==> BB_3

BB_2:
    Load 41 -> #3
    CopyValue #3 -> #0

BB_3:
    Return #0");
        }

        [Test]
        public void If_with_inner_if()
        {
            const string source = @"namespace Test;
public int32 GetTheAnswer() {
    if (true) {
        if (false) { return 0; }
        else { return 42; }
    }
    return 41;
}";
            var compiledMethod = TryCompileFirstMethod(source, out var diagnostics);

            Assert.That(compiledMethod, Is.Not.Null);
            Assert.That(diagnostics.Diagnostics, Is.Empty);
            
            AssertDisassembly(compiledMethod!, @"
; #0   bool
; #1   bool
; #2   int32
; #3   int32
; #4   int32
BB_0:
    Load true -> #0
    BranchIf #0 ==> BB_1
    ==> BB_4

BB_1:
    Load false -> #1
    BranchIf #1 ==> BB_2
    ==> BB_3

BB_2:
    Load 0 -> #2
    Return #2

BB_3:
    Load 42 -> #3
    Return #3

BB_4:
    Load 41 -> #4
    Return #4");
        }

        [Test]
        public void If_with_inner_if_in_else()
        {
            const string source = @"namespace Test;
public int32 GetTheAnswer() {
    if (false) { }
    else
    {
        if (false) { return 0; }
        else { return 42; }
    }
    return 41;
}";
            var compiledMethod = TryCompileFirstMethod(source, out var diagnostics);

            Assert.That(compiledMethod, Is.Not.Null);
            Assert.That(diagnostics.Diagnostics, Is.Empty);
            
            AssertDisassembly(compiledMethod!, @"
; #0   bool
; #1   bool
; #2   int32
; #3   int32
; #4   int32
BB_0:
    Load false -> #0
    BranchIf #0 ==> BB_1
    ==> BB_2

BB_1:
    ==> BB_5

BB_2:
    Load false -> #1
    BranchIf #1 ==> BB_3
    ==> BB_4

BB_3:
    Load 0 -> #2
    Return #2

BB_4:
    Load 42 -> #3
    Return #3

BB_5:
    Load 41 -> #4
    Return #4");
        }

        [Test]
        public void If_elseif_and_else_all_returning()
        {
            const string source = @"namespace Test;
public int32 TheAnswer() {
    if (true) {
        return 42;
    } else if (false) {
        return 41;
    }
    else {
        return 0;
    }
}";
            var compiledMethod = TryCompileFirstMethod(source, out var diagnostics);

            Assert.That(compiledMethod, Is.Not.Null);
            Assert.That(diagnostics.Diagnostics, Is.Empty);
            
            AssertDisassembly(compiledMethod!, @"
; #0   bool
; #1   int32
; #2   bool
; #3   int32
; #4   int32
BB_0:
    Load true -> #0
    BranchIf #0 ==> BB_1
    ==> BB_2

BB_1:
    Load 42 -> #1
    Return #1

BB_2:
    Load false -> #2
    BranchIf #2 ==> BB_3
    ==> BB_4

BB_3:
    Load 41 -> #3
    Return #3

BB_4:
    Load 0 -> #4
    Return #4");
        }

        [Test]
        public void If_elseif_and_else_all_assigning()
        {
            const string source = @"namespace Test;
public int32 TheAnswer() {
    int32 result = 0;
    if (true) {
        result = 42;
    } else if (false) {
        result = 41;
    }
    else {
        result = 1;
    }
    return result;
}";
            var compiledMethod = TryCompileFirstMethod(source, out var diagnostics);

            Assert.That(compiledMethod, Is.Not.Null);
            Assert.That(diagnostics.Diagnostics, Is.Empty);

            // Although BB_5 has two predecessors, the edges are not critical since BB_1 and BB_3 do not branch
            AssertDisassembly(compiledMethod!, @"
; #0   int32
; #1   bool
; #2   int32
; #3   bool
; #4   int32
; #5   int32
BB_0:
    Load 0 -> #0
    Load true -> #1
    BranchIf #1 ==> BB_1
    ==> BB_2

BB_1:
    Load 42 -> #2
    CopyValue #2 -> #0
    ==> BB_5

BB_2:
    Load false -> #3
    BranchIf #3 ==> BB_3
    ==> BB_4

BB_3:
    Load 41 -> #4
    CopyValue #4 -> #0
    ==> BB_5

BB_4:
    Load 1 -> #5
    CopyValue #5 -> #0

BB_5:
    Return #0");
        }

        [Test]
        public void If_and_elseif_returning_else_falling_through()
        {
            const string source = @"namespace Test;
public int32 TheAnswer() {
    if (true) {
        return 42;
    } else if (false) {
        return 41;
    }
    return 0;
}";
            var compiledMethod = TryCompileFirstMethod(source, out var diagnostics);

            Assert.That(compiledMethod, Is.Not.Null);
            Assert.That(diagnostics.Diagnostics, Is.Empty);
            
            AssertDisassembly(compiledMethod!, @"
; #0   bool
; #1   int32
; #2   bool
; #3   int32
; #4   int32
BB_0:
    Load true -> #0
    BranchIf #0 ==> BB_1
    ==> BB_2

BB_1:
    Load 42 -> #1
    Return #1

BB_2:
    Load false -> #2
    BranchIf #2 ==> BB_3
    ==> BB_4

BB_3:
    Load 41 -> #3
    Return #3

BB_4:
    Load 0 -> #4
    Return #4");
        }

        [Test]
        public void Condition_must_be_bool()
        {
            const string source = @"namespace Test;
public bool TypeMismatch() {
    if (42) {
        return false;
    } else {
        return true;
    }
}";
            var compiledMethod = TryCompileFirstMethod(source, out var diagnostics);

            Assert.That(compiledMethod, Is.Null);
            diagnostics.AssertDiagnosticAt(DiagnosticCode.TypeMismatch, 3, 8).WithExpected("bool").WithActual("int32");
        }

        [Test]
        public void Then_block_must_be_valid()
        {
            const string source = @"namespace Test;
public bool TypeMismatchInThen() {
    if (true) {
        return 42;
    } else {
        return true;
    }
}";
            var compiledMethod = TryCompileFirstMethod(source, out var diagnostics);

            Assert.That(compiledMethod, Is.Null);
            diagnostics.AssertDiagnosticAt(DiagnosticCode.TypeMismatch, 4, 15).WithExpected("bool").WithActual("int32");
        }

        [Test]
        public void Else_block_must_be_valid()
        {
            const string source = @"namespace Test;
public bool TypeMismatchInElse() {
    if (true) {
        return true;
    } else {
        return 42;
    }
}";
            var compiledMethod = TryCompileFirstMethod(source, out var diagnostics);

            Assert.That(compiledMethod, Is.Null);
            diagnostics.AssertDiagnosticAt(DiagnosticCode.TypeMismatch, 6, 15).WithExpected("bool").WithActual("int32");
        }

        [Test]
        public void Else_if_must_be_valid()
        {
            const string source = @"namespace Test;
public bool TypeMismatchInElseIf() {
    if (true) {
        return true;
    } else if (41) {
        return false;
    }
}";
            var compiledMethod = TryCompileFirstMethod(source, out var diagnostics);

            Assert.That(compiledMethod, Is.Null);
            diagnostics.AssertDiagnosticAt(DiagnosticCode.TypeMismatch, 5, 15).WithExpected("bool").WithActual("int32");
        }
    }
}

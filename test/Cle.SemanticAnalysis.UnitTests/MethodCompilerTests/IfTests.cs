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
            var compiledMethod = TryCompileSingleMethod(source, out var diagnostics);

            Assert.That(compiledMethod, Is.Not.Null);
            Assert.That(diagnostics.Diagnostics, Is.Empty);
            
            AssertDisassembly(compiledMethod, @"
; #0   bool = true
; #1   bool = false
; #2   bool = true
BB_0:
    BranchIf #0 ==> BB_1
    ==> BB_2

BB_1:
    Return #1

BB_2:
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
            var compiledMethod = TryCompileSingleMethod(source, out var diagnostics);

            Assert.That(compiledMethod, Is.Not.Null);
            Assert.That(diagnostics.Diagnostics, Is.Empty);
            
            AssertDisassembly(compiledMethod, @"
; #0   bool = true
; #1   bool = true
; #2   bool = false
BB_0:
    BranchIf #0 ==> BB_1
    ==> BB_2

BB_1:
    Return #1

BB_2:
    Return #2");
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
            var compiledMethod = TryCompileSingleMethod(source, out var diagnostics);

            Assert.That(compiledMethod, Is.Not.Null);
            Assert.That(diagnostics.Diagnostics, Is.Empty);
            
            AssertDisassembly(compiledMethod, @"
; #0   int32 = 42
; #1   bool = false
; #2   int32 = 1
BB_0:
    BranchIf #1 ==> BB_1
    ==> BB_2

BB_1:
    CopyValue #2 -> #0

BB_2:
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
            var compiledMethod = TryCompileSingleMethod(source, out var diagnostics);

            Assert.That(compiledMethod, Is.Not.Null);
            Assert.That(diagnostics.Diagnostics, Is.Empty);

            AssertDisassembly(compiledMethod, @"
; #0   int32 = 0
; #1   bool = true
; #2   int32 = 42
; #3   int32 = 41
BB_0:
    BranchIf #1 ==> BB_1
    ==> BB_2

BB_1:
    CopyValue #2 -> #0
    ==> BB_3

BB_2:
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
            var compiledMethod = TryCompileSingleMethod(source, out var diagnostics);

            Assert.That(compiledMethod, Is.Not.Null);
            Assert.That(diagnostics.Diagnostics, Is.Empty);
            
            AssertDisassembly(compiledMethod, @"
; #0   bool = true
; #1   bool = false
; #2   int32 = 0
; #3   int32 = 42
; #4   int32 = 41
BB_0:
    BranchIf #0 ==> BB_1
    ==> BB_5

BB_1:
    BranchIf #1 ==> BB_2
    ==> BB_3

BB_2:
    Return #2

BB_3:
    Return #3

BB_5:
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
            var compiledMethod = TryCompileSingleMethod(source, out var diagnostics);

            Assert.That(compiledMethod, Is.Not.Null);
            Assert.That(diagnostics.Diagnostics, Is.Empty);
            
            AssertDisassembly(compiledMethod, @"
; #0   bool = false
; #1   bool = false
; #2   int32 = 0
; #3   int32 = 42
; #4   int32 = 41
BB_0:
    BranchIf #0 ==> BB_1
    ==> BB_2

BB_1:
    ==> BB_6

BB_2:
    BranchIf #1 ==> BB_3
    ==> BB_4

BB_3:
    Return #2

BB_4:
    Return #3

BB_6:
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
            var compiledMethod = TryCompileSingleMethod(source, out var diagnostics);

            Assert.That(compiledMethod, Is.Not.Null);
            Assert.That(diagnostics.Diagnostics, Is.Empty);
            
            AssertDisassembly(compiledMethod, @"
; #0   bool = true
; #1   int32 = 42
; #2   bool = false
; #3   int32 = 41
; #4   int32 = 0
BB_0:
    BranchIf #0 ==> BB_1
    ==> BB_2

BB_1:
    Return #1

BB_2:
    BranchIf #2 ==> BB_3
    ==> BB_4

BB_3:
    Return #3

BB_4:
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
            var compiledMethod = TryCompileSingleMethod(source, out var diagnostics);

            Assert.That(compiledMethod, Is.Not.Null);
            Assert.That(diagnostics.Diagnostics, Is.Empty);
            
            AssertDisassembly(compiledMethod, @"
; #0   int32 = 0
; #1   bool = true
; #2   int32 = 42
; #3   bool = false
; #4   int32 = 41
; #5   int32 = 1
BB_0:
    BranchIf #1 ==> BB_1
    ==> BB_2

BB_1:
    CopyValue #2 -> #0
    ==> BB_5

BB_2:
    BranchIf #3 ==> BB_3
    ==> BB_4

BB_3:
    CopyValue #4 -> #0
    ==> BB_5

BB_4:
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
            var compiledMethod = TryCompileSingleMethod(source, out var diagnostics);

            Assert.That(compiledMethod, Is.Not.Null);
            Assert.That(diagnostics.Diagnostics, Is.Empty);
            
            AssertDisassembly(compiledMethod, @"
; #0   bool = true
; #1   int32 = 42
; #2   bool = false
; #3   int32 = 41
; #4   int32 = 0
BB_0:
    BranchIf #0 ==> BB_1
    ==> BB_2

BB_1:
    Return #1

BB_2:
    BranchIf #2 ==> BB_3
    ==> BB_4

BB_3:
    Return #3

BB_4:
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
            var compiledMethod = TryCompileSingleMethod(source, out var diagnostics);

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
            var compiledMethod = TryCompileSingleMethod(source, out var diagnostics);

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
            var compiledMethod = TryCompileSingleMethod(source, out var diagnostics);

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
            var compiledMethod = TryCompileSingleMethod(source, out var diagnostics);

            Assert.That(compiledMethod, Is.Null);
            diagnostics.AssertDiagnosticAt(DiagnosticCode.TypeMismatch, 5, 15).WithExpected("bool").WithActual("int32");
        }
    }
}

using Cle.Common;
using NUnit.Framework;

namespace Cle.SemanticAnalysis.UnitTests.MethodCompilerTests
{
    public class ReturnGuaranteeTests : MethodCompilerTestBase
    {
        [Test]
        public void Method_without_return_fails()
        {
            const string source = @"namespace Test;
public int32 Fail() {
}";
            var compiledMethod = TryCompileFirstMethod(source, out var diagnostics);

            Assert.That(compiledMethod, Is.Null);
            Assert.That(diagnostics.Diagnostics, Has.Exactly(1).Items);
            diagnostics.AssertDiagnosticAt(DiagnosticCode.ReturnNotGuaranteed, 2, 0).WithActual("Fail");
        }

        [Test]
        public void Method_without_return_in_one_branch_fails()
        {
            const string source = @"namespace Test;
public int32 Fail() {
    var bool shouldReturn = false;
    if (shouldReturn) { return 1; }
}";
            var compiledMethod = TryCompileFirstMethod(source, out var diagnostics);

            Assert.That(compiledMethod, Is.Null);
            Assert.That(diagnostics.Diagnostics, Has.Exactly(1).Items);
            diagnostics.AssertDiagnosticAt(DiagnosticCode.ReturnNotGuaranteed, 2, 0).WithActual("Fail");
        }

        [Test]
        public void Method_without_return_in_else_if_branch_fails()
        {
            const string source = @"namespace Test;
public int32 Fail() {
    var bool shouldReturn = false;
    var bool shouldReturnInElse = false;
    if (shouldReturn) { return 1; }
    else if (shouldReturnInElse) { return 2; }
    else {}
}";
            var compiledMethod = TryCompileFirstMethod(source, out var diagnostics);

            Assert.That(compiledMethod, Is.Null);
            Assert.That(diagnostics.Diagnostics, Has.Exactly(1).Items);
            diagnostics.AssertDiagnosticAt(DiagnosticCode.ReturnNotGuaranteed, 2, 0).WithActual("Fail");
        }

        [Test]
        public void Method_with_unreachable_block_causes_warning()
        {
            const string source = @"namespace Test;
public int32 Warn() {
    return 1;
    { }
}";
            var compiledMethod = TryCompileFirstMethod(source, out var diagnostics);

            Assert.That(compiledMethod, Is.Not.Null);
            Assert.That(diagnostics.Diagnostics, Has.Exactly(1).Items);
            diagnostics.AssertDiagnosticAt(DiagnosticCode.UnreachableCode, 4, 4);
        }

        [Test]
        public void Dead_code_warning_is_emitted_once_per_block()
        {
            const string source = @"namespace Test;
public int32 Warn() {
    return 1;
    return 2;
    return 3;
}";
            var compiledMethod = TryCompileFirstMethod(source, out var diagnostics);

            Assert.That(compiledMethod, Is.Not.Null);
            Assert.That(diagnostics.Diagnostics, Has.Exactly(1).Items);
            diagnostics.AssertDiagnosticAt(DiagnosticCode.UnreachableCode, 4, 4);
        }

        [Test]
        public void Method_with_return_in_nested_block_causes_two_warnings()
        {
            const string source = @"namespace Test;
public int32 Warn() {
    {
        return 1;
        return 2;
    }
    return 3;
}";
            var compiledMethod = TryCompileFirstMethod(source, out var diagnostics);

            Assert.That(compiledMethod, Is.Not.Null);
            Assert.That(diagnostics.Diagnostics, Has.Exactly(2).Items);
            diagnostics.AssertDiagnosticAt(DiagnosticCode.UnreachableCode, 5, 8);
            diagnostics.AssertDiagnosticAt(DiagnosticCode.UnreachableCode, 7, 4);
        }

        [Test]
        public void Method_with_return_in_two_if_branches_causes_warning()
        {
            const string source = @"namespace Test;
public int32 Warn() {
    var bool isTrue = true;
    if (isTrue) { return 1; }
    else { return 2; }
    return 3;
}";
            var compiledMethod = TryCompileFirstMethod(source, out var diagnostics);

            Assert.That(compiledMethod, Is.Not.Null);
            Assert.That(diagnostics.Diagnostics, Has.Exactly(1).Items);
            diagnostics.AssertDiagnosticAt(DiagnosticCode.UnreachableCode, 6, 4);
        }

        [Test]
        public void Method_with_return_in_three_if_branches_causes_warning()
        {
            const string source = @"namespace Test;
public int32 Warn() {
    var bool isTrue = true;
    var bool isFalse = false;
    if (isTrue) { return 1; }
    else if (isFalse) { return 2; }
    else { return 3; }
    return 4;
}";
            var compiledMethod = TryCompileFirstMethod(source, out var diagnostics);

            Assert.That(compiledMethod, Is.Not.Null);
            Assert.That(diagnostics.Diagnostics, Has.Exactly(1).Items);
            diagnostics.AssertDiagnosticAt(DiagnosticCode.UnreachableCode, 8, 4);
        }

        [Test]
        public void Method_with_return_only_in_while_fails()
        {
            const string source = @"namespace Test;
public int32 Fail() {
    var bool a = true;
    while (a) { return 1; }
}";
            var compiledMethod = TryCompileFirstMethod(source, out var diagnostics);

            Assert.That(compiledMethod, Is.Null);
            Assert.That(diagnostics.Diagnostics, Has.Exactly(1).Items);
            diagnostics.AssertDiagnosticAt(DiagnosticCode.ReturnNotGuaranteed, 2, 0).WithActual("Fail");
        }

        [Test]
        public void Method_with_return_in_while_does_not_cause_warning()
        {
            const string source = @"namespace Test;
public int32 Warn() {
    var bool a = true;
    while (a) { return 1; }
    return 4;
}";
            var compiledMethod = TryCompileFirstMethod(source, out var diagnostics);

            Assert.That(compiledMethod, Is.Not.Null);
            Assert.That(diagnostics.Diagnostics, Is.Empty);
        }
    }
}

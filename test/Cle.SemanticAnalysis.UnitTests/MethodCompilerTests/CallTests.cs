using Cle.Common;
using NUnit.Framework;

namespace Cle.SemanticAnalysis.UnitTests.MethodCompilerTests
{
    public class CallTests : MethodCompilerTestBase
    {
        [Test]
        public void Standalone_method_calls_to_parameterless_void_succeeds()
        {
            const string source = @"namespace Test::Namespace;
public void CallVoid()
{
    VoidMethod(); // Simple name
    Test::Namespace::VoidMethod(); // Full name
}

private void VoidMethod() {}";
            var compiledMethod = TryCompileFirstMethod(source, out var diagnostics);

            Assert.That(diagnostics.Diagnostics, Is.Empty);
            Assert.That(compiledMethod, Is.Not.Null);
            
            AssertDisassembly(compiledMethod!, @"
; #0   void
; #1   void
; #2   void
BB_0:
    Call Test::Namespace::VoidMethod() -> #0
    Call Test::Namespace::VoidMethod() -> #1
    Return #2");
        }

        [Test]
        public void Standalone_method_call_to_imported_method_succeeds()
        {
            const string source = @"namespace Test::Namespace;
public void CallImported()
{
    FromSomewhere();
}

[Import(""some_method"", ""SomeLibrary.dll"")]
private void FromSomewhere() {}";
            var compiledMethod = TryCompileFirstMethod(source, out var diagnostics);

            Assert.That(diagnostics.Diagnostics, Is.Empty);
            Assert.That(compiledMethod, Is.Not.Null);
            
            AssertDisassembly(compiledMethod!, @"
; #0   void
; #1   void
BB_0:
    Call Test::Namespace::FromSomewhere() import -> #0
    Return #1");
        }

        [Test]
        public void Standalone_method_call_to_parameterized_bool_succeeds()
        {
            const string source = @"namespace Test;
public void CallBool()
{
    IsLarger(1, 2);
}

private bool IsLarger(int32 left, int32 right) { return left > right; }";
            var compiledMethod = TryCompileFirstMethod(source, out var diagnostics);

            Assert.That(diagnostics.Diagnostics, Is.Empty);
            Assert.That(compiledMethod, Is.Not.Null);
            
            AssertDisassembly(compiledMethod!, @"
; #0   int32
; #1   int32
; #2   bool
; #3   void
BB_0:
    Load 1 -> #0
    Load 2 -> #1
    Call Test::IsLarger(#0, #1) -> #2
    Return #3");
        }

        [Test]
        public void Method_call_in_expression_to_parameterized_bool_succeeds()
        {
            const string source = @"namespace Test;
public bool CallBool()
{
    return IsLarger(1, 2);
}

private bool IsLarger(int32 left, int32 right) { return left > right; }";
            var compiledMethod = TryCompileFirstMethod(source, out var diagnostics);

            Assert.That(diagnostics.Diagnostics, Is.Empty);
            Assert.That(compiledMethod, Is.Not.Null);
            
            AssertDisassembly(compiledMethod!, @"
; #0   int32
; #1   int32
; #2   bool
BB_0:
    Load 1 -> #0
    Load 2 -> #1
    Call Test::IsLarger(#0, #1) -> #2
    Return #2");
        }

        [Test]
        public void Method_call_in_expression_with_run_time_evaluated_parameter_succeeds()
        {
            const string source = @"namespace Test;
public bool CallBool(int32 value)
{
    return IsLarger(1, value + value);
}

private bool IsLarger(int32 left, int32 right) { return left > right; }";
            var compiledMethod = TryCompileFirstMethod(source, out var diagnostics);

            Assert.That(diagnostics.Diagnostics, Is.Empty);
            Assert.That(compiledMethod, Is.Not.Null);
            
            AssertDisassembly(compiledMethod!, @"
; #0   int32 param
; #1   int32
; #2   int32
; #3   bool
BB_0:
    Load 1 -> #1
    Add #0 + #0 -> #2
    Call Test::IsLarger(#1, #2) -> #3
    Return #3");
        }

        [Test]
        public void Callee_must_be_found()
        {
            const string source = @"namespace Test;
public void NotFound()
{
    return Nonexistent();
}";
            var compiledMethod = TryCompileFirstMethod(source, out var diagnostics);

            Assert.That(compiledMethod, Is.Null);
            diagnostics.AssertDiagnosticAt(DiagnosticCode.MethodNotFound, 4, 11)
                .WithActual("Nonexistent");
        }

        [Test]
        public void Callee_with_full_name_must_be_found()
        {
            const string source = @"namespace Test;
public void NotFound()
{
    return More::Something();
}";
            var compiledMethod = TryCompileFirstMethod(source, out var diagnostics);

            Assert.That(compiledMethod, Is.Null);
            diagnostics.AssertDiagnosticAt(DiagnosticCode.MethodNotFound, 4, 11)
                .WithActual("More::Something");
        }

        [Test]
        public void Method_call_in_expression_result_type_must_be_correct()
        {
            const string source = @"namespace Test;
public void TypeMismatch()
{
    return BoolMethod();
}

private bool BoolMethod() { return true; }";
            var compiledMethod = TryCompileFirstMethod(source, out var diagnostics);

            Assert.That(compiledMethod, Is.Null);
            diagnostics.AssertDiagnosticAt(DiagnosticCode.TypeMismatch, 4, 11)
                .WithActual("bool").WithExpected("void");
        }

        [Test]
        public void Method_call_in_expression_parameter_type_must_be_correct()
        {
            const string source = @"namespace Test;
public bool TypeMismatch()
{
    return IsLarger(1, true);
}

private bool IsLarger(int32 left, int32 right) { return left > right; }";
            var compiledMethod = TryCompileFirstMethod(source, out var diagnostics);

            Assert.That(compiledMethod, Is.Null);
            diagnostics.AssertDiagnosticAt(DiagnosticCode.TypeMismatch, 4, 23)
                .WithActual("bool").WithExpected("int32");
        }

        [Test]
        public void Method_call_in_expression_must_have_enough_parameters()
        {
            const string source = @"namespace Test;
public bool TypeMismatch()
{
    return IsLarger(1);
}

private bool IsLarger(int32 left, int32 right) { return left > right; }";
            var compiledMethod = TryCompileFirstMethod(source, out var diagnostics);

            Assert.That(compiledMethod, Is.Null);
            diagnostics.AssertDiagnosticAt(DiagnosticCode.ParameterCountMismatch, 4, 11)
                .WithActual("1").WithExpected("2");
        }

        [Test]
        public void Method_call_in_expression_must_not_have_too_many_parameters()
        {
            const string source = @"namespace Test;
public bool TypeMismatch()
{
    return IsLarger(1, 2, 3);
}

private bool IsLarger(int32 left, int32 right) { return left > right; }";
            var compiledMethod = TryCompileFirstMethod(source, out var diagnostics);

            Assert.That(compiledMethod, Is.Null);
            diagnostics.AssertDiagnosticAt(DiagnosticCode.ParameterCountMismatch, 4, 11)
                .WithActual("3").WithExpected("2");
        }
    }
}

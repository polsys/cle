using Cle.Common;
using NUnit.Framework;

namespace Cle.SemanticAnalysis.UnitTests.MethodCompilerTests
{
    public class VariableTests : MethodCompilerTestBase
    {
        [Test]
        public void Assignment_from_variable_to_itself()
        {
            const string source = @"namespace Test;
public void DoNothing() {
    var int32 a = 7;
    a = a;
    return;
}";
            var compiledMethod = TryCompileFirstMethod(source, out var diagnostics);

            Assert.That(compiledMethod, Is.Not.Null);
            Assert.That(diagnostics.Diagnostics, Is.Empty);
            
            AssertDisassembly(compiledMethod!, @"
; #0   int32
; #1   void
BB_0:
    Load 7 -> #0
    CopyValue #0 -> #0
    Return #1");
        }

        [Test]
        public void Reassigning_a_variable()
        {
            const string source = @"namespace Test;
public void DoNothing() {
    var int32 a = 7;
    a = 8;
    return;
}";
            var compiledMethod = TryCompileFirstMethod(source, out var diagnostics);

            Assert.That(compiledMethod, Is.Not.Null);
            Assert.That(diagnostics.Diagnostics, Is.Empty);
            
            AssertDisassembly(compiledMethod!, @"
; #0   int32
; #1   int32
; #2   void
BB_0:
    Load 7 -> #0
    Load 8 -> #1
    CopyValue #1 -> #0
    Return #2");
        }

        [Test]
        public void Assignment_from_variable_to_another_variable_in_initialization()
        {
            const string source = @"namespace Test;
public void DoNothing() {
    var int32 a = 7;
    var int32 b = a;
    return;
}";
            var compiledMethod = TryCompileFirstMethod(source, out var diagnostics);

            Assert.That(compiledMethod, Is.Not.Null);
            Assert.That(diagnostics.Diagnostics, Is.Empty);
            
            // void will be used for the initial value
            AssertDisassembly(compiledMethod!, @"
; #0   int32
; #1   int32
; #2   void
BB_0:
    Load 7 -> #0
    CopyValue #0 -> #1
    Return #2");
        }

        [Test]
        public void Variable_not_found_in_left_side_of_assignment()
        {
            const string source = @"namespace Test;
public void NotFound() {
    notFound = 404;
    return;
}";
            var compiledMethod = TryCompileFirstMethod(source, out var diagnostics);

            Assert.That(compiledMethod, Is.Null);
            diagnostics.AssertDiagnosticAt(DiagnosticCode.VariableNotFound, 3, 4).WithActual("notFound");
        }

        [Test]
        public void Variable_not_found_in_right_side_of_assignment()
        {
            const string source = @"namespace Test;
public void NotFound() {
    var int32 target = 0;
    target = notFound;
    return;
}";
            var compiledMethod = TryCompileFirstMethod(source, out var diagnostics);

            Assert.That(compiledMethod, Is.Null);
            diagnostics.AssertDiagnosticAt(DiagnosticCode.VariableNotFound, 4, 13).WithActual("notFound");
        }

        [Test]
        public void Wrong_type_in_assignment()
        {
            const string source = @"namespace Test;
public void WrongType() {
    var int32 target = 0;
    var bool value = true;
    target = value;
    return;
}";
            var compiledMethod = TryCompileFirstMethod(source, out var diagnostics);

            Assert.That(compiledMethod, Is.Null);
            diagnostics.AssertDiagnosticAt(DiagnosticCode.TypeMismatch, 5, 13)
                .WithExpected("int32").WithActual("bool");
        }

        [Test]
        public void Wrong_type_in_initialization()
        {
            const string source = @"namespace Test;
public void WrongType() {
    var int32 target = true;
    return;
}";
            var compiledMethod = TryCompileFirstMethod(source, out var diagnostics);

            Assert.That(compiledMethod, Is.Null);
            diagnostics.AssertDiagnosticAt(DiagnosticCode.TypeMismatch, 3, 23)
                .WithExpected("int32").WithActual("bool");
        }

        [Test]
        public void Variable_declaration_may_not_refer_to_itself()
        {
            const string source = @"namespace Test;
public void SelfReference() {
    var int32 cyclic = cyclic;
    return;
}";
            var compiledMethod = TryCompileFirstMethod(source, out var diagnostics);

            Assert.That(compiledMethod, Is.Null);
            diagnostics.AssertDiagnosticAt(DiagnosticCode.VariableNotFound, 3, 23).WithActual("cyclic");
        }

        [Test]
        public void Variable_declaration_with_nonexistent_type()
        {
            const string source = @"namespace Test;
public void Huh() {
    var Whatever value = 42;
    return;
}";
            var compiledMethod = TryCompileFirstMethod(source, out var diagnostics);

            Assert.That(compiledMethod, Is.Null);
            diagnostics.AssertDiagnosticAt(DiagnosticCode.TypeNotFound, 3, 8).WithActual("Whatever");
        }

        [Test]
        public void Variable_name_may_not_be_reused_in_same_scope()
        {
            const string source = @"namespace Test;
public void NameAlreadyDefined() {
    var int32 conflict = 7;
    var int32 conflict = 8;
    return;
}";
            var compiledMethod = TryCompileFirstMethod(source, out var diagnostics);

            Assert.That(compiledMethod, Is.Null);
            diagnostics.AssertDiagnosticAt(DiagnosticCode.VariableAlreadyDefined, 4, 4).WithActual("conflict");
        }

        [Test]
        public void Variable_name_may_not_be_reused_in_enclosed_scope()
        {
            const string source = @"namespace Test;
public void NameAlreadyDefined() {
    var int32 conflict = 7;
    {
        var int32 conflict = 8;
    }
    return;
}";
            var compiledMethod = TryCompileFirstMethod(source, out var diagnostics);

            Assert.That(compiledMethod, Is.Null);
            diagnostics.AssertDiagnosticAt(DiagnosticCode.VariableAlreadyDefined, 5, 8).WithActual("conflict");
        }

        [Test]
        public void Variable_name_may_be_reused_in_separate_scopes()
        {
            const string source = @"namespace Test;
public void NameAppearsTwice() {
    {
        var int32 conflict = 7;
    }
    {
        var int32 conflict = 8;
    }
    return;
}";
            var compiledMethod = TryCompileFirstMethod(source, out var diagnostics);

            Assert.That(compiledMethod, Is.Not.Null);
            Assert.That(diagnostics.Diagnostics, Is.Empty);

            AssertDisassembly(compiledMethod!, @"
; #0   int32
; #1   int32
; #2   void
BB_0:
    Load 7 -> #0
    Load 8 -> #1
    Return #2");
        }

        [Test]
        public void Variable_name_may_be_defined_later_in_enclosing_scope()
        {
            // TODO: Consider whether this should be removed, like it is done in C#.
            // TODO: At the moment it is a limitation of the compiler.
            const string source = @"namespace Test;
public void NameAppearsTwice() {
    {
        var int32 conflict = 7;
    }

    var int32 conflict = 8;
    return;
}";
            var compiledMethod = TryCompileFirstMethod(source, out var diagnostics);

            Assert.That(compiledMethod, Is.Not.Null);
            Assert.That(diagnostics.Diagnostics, Is.Empty);

            AssertDisassembly(compiledMethod!, @"
; #0   int32
; #1   int32
; #2   void
BB_0:
    Load 7 -> #0
    Load 8 -> #1
    Return #2");
        }

        [Test]
        public void Parameters_are_variables()
        {
            const string source = @"namespace Test;
public int32 Params(int32 first, bool second) {
    var int32 more = 3;
    return first;
}";
            var compiledMethod = TryCompileFirstMethod(source, out var diagnostics);

            Assert.That(diagnostics.Diagnostics, Is.Empty);
            Assert.That(compiledMethod, Is.Not.Null);

            AssertDisassembly(compiledMethod!, @"
; #0   int32 param
; #1   bool param
; #2   int32
BB_0:
    Load 3 -> #2
    Return #0");
        }

        [Test]
        public void Parameter_name_may_not_be_repeated()
        {
            const string source = @"namespace Test;
public void NameAlreadyDefined(int32 variable, bool variable) {}";
            var compiledMethod = TryCompileFirstMethod(source, out var diagnostics);

            Assert.That(compiledMethod, Is.Null);
            diagnostics.AssertDiagnosticAt(DiagnosticCode.VariableAlreadyDefined, 2, 47).WithActual("variable");
        }

        [Test]
        public void Variable_may_not_have_same_name_as_parameter()
        {
            const string source = @"namespace Test;
public void NameAlreadyDefined(int32 variable) {
    var bool variable = true;
}";
            var compiledMethod = TryCompileFirstMethod(source, out var diagnostics);

            Assert.That(compiledMethod, Is.Null);
            diagnostics.AssertDiagnosticAt(DiagnosticCode.VariableAlreadyDefined, 3, 4).WithActual("variable");
        }

        [Test]
        public void Variable_may_not_be_void()
        {
            const string source = @"namespace Test;
public void VoidVar() {
    var void variable = VoidVar();
}";
            var compiledMethod = TryCompileFirstMethod(source, out var diagnostics);

            Assert.That(compiledMethod, Is.Null);
            diagnostics.AssertDiagnosticAt(DiagnosticCode.VoidIsNotValidType, 3, 4).WithActual("variable");
        }
    }
}

using Cle.Common;
using Cle.Common.TypeSystem;
using Cle.Parser.SyntaxTree;
using Cle.SemanticAnalysis.IR;
using Cle.UnitTests.Common;
using NUnit.Framework;

namespace Cle.SemanticAnalysis.UnitTests
{
    public class ExpressionCompilerTests
    {
        [Test]
        public void Integer_literal_stored_in_int32_succeeds()
        {
            var syntax = new IntegerLiteralSyntax(1234, default);
            var method = new CompiledMethod();
            var builder = new BasicBlockGraphBuilder().GetInitialBlockBuilder();
            var diagnostics = new TestingDiagnosticSink();

            var localIndex = ExpressionCompiler.TryCompileExpression(
                syntax, SimpleType.Int32, method, builder, diagnostics);

            Assert.That(localIndex, Is.EqualTo(0));
            Assert.That(diagnostics.Diagnostics, Is.Empty);
            Assert.That(method.Values, Has.Exactly(1).Items);

            var local = method.Values[0];
            Assert.That(local.Type, Is.EqualTo(SimpleType.Int32));
            Assert.That(local.InitialValue, Is.EqualTo(ConstantValue.SignedInteger(1234)));
        }

        [Test]
        public void Integer_literal_stored_in_bool_fails()
        {
            var position = new TextPosition(10, 3, 4);
            var syntax = new IntegerLiteralSyntax(1234, position);
            var method = new CompiledMethod();
            var builder = new BasicBlockGraphBuilder().GetInitialBlockBuilder();
            var diagnostics = new TestingDiagnosticSink();

            var localIndex = ExpressionCompiler.TryCompileExpression(
                syntax, SimpleType.Bool, method, builder, diagnostics);

            Assert.That(localIndex, Is.EqualTo(-1));
            diagnostics.AssertDiagnosticAt(DiagnosticCode.TypeMismatch, position)
                .WithActual("int32")
                .WithExpected("bool");
        }

        [Test]
        public void Integer_literal_that_is_too_large_fails()
        {
            var position = new TextPosition(10, 3, 4);
            var syntax = new IntegerLiteralSyntax((ulong)int.MaxValue + 1, position);
            var method = new CompiledMethod();
            var builder = new BasicBlockGraphBuilder().GetInitialBlockBuilder();
            var diagnostics = new TestingDiagnosticSink();

            var localIndex = ExpressionCompiler.TryCompileExpression(
                syntax, SimpleType.Int32, method, builder, diagnostics);

            Assert.That(localIndex, Is.EqualTo(-1));
            diagnostics.AssertDiagnosticAt(DiagnosticCode.IntegerLiteralOutOfBounds, position)
                .WithActual(((ulong)int.MaxValue + 1).ToString())
                .WithExpected("int32");
        }

        [Test]
        public void Boolean_literal_stored_in_bool_succeeds()
        {
            var syntax = new BooleanLiteralSyntax(true, default);
            var method = new CompiledMethod();
            var builder = new BasicBlockGraphBuilder().GetInitialBlockBuilder();
            var diagnostics = new TestingDiagnosticSink();

            var localIndex = ExpressionCompiler.TryCompileExpression(
                syntax, SimpleType.Bool, method, builder, diagnostics);

            Assert.That(localIndex, Is.EqualTo(0));
            Assert.That(diagnostics.Diagnostics, Is.Empty);
            Assert.That(method.Values, Has.Exactly(1).Items);

            var local = method.Values[0];
            Assert.That(local.Type, Is.EqualTo(SimpleType.Bool));
            Assert.That(local.InitialValue, Is.EqualTo(ConstantValue.Bool(true)));
        }
    }
}

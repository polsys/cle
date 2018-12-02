using Cle.Common.TypeSystem;
using Cle.SemanticAnalysis.IR;
using NUnit.Framework;

namespace Cle.SemanticAnalysis.UnitTests
{
    public class CompiledMethodTests
    {
        [Test]
        public void CreateTemporary_succeeds_for_int32()
        {
            var method = new CompiledMethod();

            Assert.That(method.AddTemporary(SimpleType.Int32, ConstantValue.SignedInteger(1)), Is.EqualTo(0));
            Assert.That(method.AddTemporary(SimpleType.Int32, ConstantValue.SignedInteger(-17)), Is.EqualTo(1));

            Assert.That(method.Values, Has.Exactly(2).Items);
            Assert.That(method.Values[0].Type, Is.EqualTo(SimpleType.Int32));
            Assert.That(method.Values[0].InitialValue.AsSignedInteger, Is.EqualTo(1));
            Assert.That(method.Values[1].Type, Is.EqualTo(SimpleType.Int32));
            Assert.That(method.Values[1].InitialValue.AsSignedInteger, Is.EqualTo(-17));
        }

        [Test]
        public void CreateTemporary_succeeds_for_bool()
        {
            var method = new CompiledMethod();

            Assert.That(method.AddTemporary(SimpleType.Bool, ConstantValue.Bool(true)), Is.EqualTo(0));
            Assert.That(method.AddTemporary(SimpleType.Bool, ConstantValue.Bool(false)), Is.EqualTo(1));

            Assert.That(method.Values, Has.Exactly(2).Items);
            Assert.That(method.Values[0].Type, Is.EqualTo(SimpleType.Bool));
            Assert.That(method.Values[0].InitialValue.AsBool, Is.EqualTo(true));
            Assert.That(method.Values[1].Type, Is.EqualTo(SimpleType.Bool));
            Assert.That(method.Values[1].InitialValue.AsBool, Is.EqualTo(false));
        }
    }
}

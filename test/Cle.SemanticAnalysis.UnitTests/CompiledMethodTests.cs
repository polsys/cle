using Cle.Common.TypeSystem;
using Cle.SemanticAnalysis.IR;
using NUnit.Framework;

namespace Cle.SemanticAnalysis.UnitTests
{
    public class CompiledMethodTests
    {
        [Test]
        public void CreateLocal_succeeds_for_int32()
        {
            var method = new CompiledMethod("Test::Method");

            Assert.That(method.AddLocal(SimpleType.Int32, LocalFlags.None), Is.EqualTo(0));
            Assert.That(method.AddLocal(SimpleType.Int32, LocalFlags.Parameter), Is.EqualTo(1));

            Assert.That(method.Values, Has.Exactly(2).Items);
            Assert.That(method.Values[0].Type, Is.EqualTo(SimpleType.Int32));
            Assert.That(method.Values[0].Flags, Is.EqualTo(LocalFlags.None));
            Assert.That(method.Values[1].Type, Is.EqualTo(SimpleType.Int32));
            Assert.That(method.Values[1].Flags, Is.EqualTo(LocalFlags.Parameter));
        }

        [Test]
        public void CreateLocal_succeeds_for_bool()
        {
            var method = new CompiledMethod("Test::Method");

            Assert.That(method.AddLocal(SimpleType.Bool, LocalFlags.None), Is.EqualTo(0));
            Assert.That(method.AddLocal(SimpleType.Bool, LocalFlags.Parameter), Is.EqualTo(1));

            Assert.That(method.Values, Has.Exactly(2).Items);
            Assert.That(method.Values[0].Type, Is.EqualTo(SimpleType.Bool));
            Assert.That(method.Values[0].Flags, Is.EqualTo(LocalFlags.None));
            Assert.That(method.Values[1].Type, Is.EqualTo(SimpleType.Bool));
            Assert.That(method.Values[1].Flags, Is.EqualTo(LocalFlags.Parameter));
        }
    }
}

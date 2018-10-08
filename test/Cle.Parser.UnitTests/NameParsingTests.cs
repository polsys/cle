using NUnit.Framework;

namespace Cle.Parser.UnitTests
{
    public class NameParsingTests
    {
        [TestCase("Namespace")]
        [TestCase("Multiple::Part::Name")]
        [TestCase("_ns")]
        [TestCase("_ns::_1::abc123")]
        [TestCase("__")]
        public void IsValidNamespaceName_valid(string name)
        {
            Assert.That(NameParsing.IsValidNamespaceName(name), Is.True);
        }

        [TestCase("::Namespace")]
        [TestCase("Namespace::")]
        [TestCase("Missing::::Part")]
        [TestCase("Missing:Separator")]
        [TestCase("_")]
        [TestCase("3D")]
        [TestCase("Math::3D")]
        [TestCase("")]
        public void IsValidNamespaceName_invalid(string name)
        {
            Assert.That(NameParsing.IsValidNamespaceName(name), Is.False);
        }

        [TestCase("simpleName")]
        [TestCase("Simple_name")]
        [TestCase("_simpleName")]
        [TestCase("simple1")]
        [TestCase("i")]
        [TestCase("_9")]
        [TestCase("__")]
        public void IsValidSimpleName_valid(string name)
        {
            Assert.That(NameParsing.IsValidSimpleName(name), Is.True);
        }

        [TestCase("_")]
        [TestCase("1")]
        [TestCase("1_")]
        [TestCase("simpleNam\x00E9")]
        [TestCase("a-b")]
        [TestCase("")]
        public void IsValidSimpleName_invalid(string name)
        {
            Assert.That(NameParsing.IsValidSimpleName(name), Is.False);
        }

        [TestCase("simpleName")]
        [TestCase("Namespace::_9")]
        [TestCase("__::____::__")]
        public void IsValidFullName_valid(string name)
        {
            Assert.That(NameParsing.IsValidFullName(name), Is.True);
        }

        [TestCase("_")]
        [TestCase("Namespace::_")]
        [TestCase("::Name")]
        [TestCase("Name::")]
        [TestCase("Other::Nam\x00E9space::Type")]
        public void IsValidFullName_invalid(string name)
        {
            Assert.That(NameParsing.IsValidFullName(name), Is.False);
        }
    }
}

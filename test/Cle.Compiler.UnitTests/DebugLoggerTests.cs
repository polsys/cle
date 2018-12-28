using System.IO;
using Cle.SemanticAnalysis.IR;
using NUnit.Framework;

namespace Cle.Compiler.UnitTests
{
    public class DebugLoggerTests
    {
        [Test]
        public void Method_name_is_matched()
        {
            var writer = new StringWriter();
            var logger = new DebugLogger(writer, "a"); // Match names containing "a"
            
            Assert.That(logger.ShouldLog("a"), Is.True);
            Assert.That(logger.ShouldLog("A"), Is.True); // Case-insensitivity
            Assert.That(logger.ShouldLog("Long::Namespace::Name::Function"), Is.True);
            Assert.That(logger.ShouldLog("function2"), Is.False);
        }

        [Test]
        public void Wildcard_matches_all_methods()
        {
            var writer = new StringWriter();
            var logger = new DebugLogger(writer, "*");
            
            Assert.That(logger.ShouldLog("a"), Is.True);
            Assert.That(logger.ShouldLog("Long::Namespace::Name::Function"), Is.True);
            Assert.That(logger.ShouldLog("function2"), Is.True);
        }

        [Test]
        public void Null_writer_disables_logging()
        {
            var logger = new DebugLogger(null, "*");

            Assert.That(logger.ShouldLog("a"), Is.False);
            Assert.That(() => logger.WriteHeader("a"), Throws.Nothing);
            Assert.That(() => logger.WriteLine("a"), Throws.Nothing);
            Assert.That(() => logger.DumpMethod(new CompiledMethod("a")), Throws.Nothing);
        }

        [Test]
        public void Null_writer_and_null_pattern_disable_logging()
        {
            var logger = new DebugLogger(null, null);

            Assert.That(logger.ShouldLog("a"), Is.False);
            Assert.That(() => logger.WriteHeader("a"), Throws.Nothing);
            Assert.That(() => logger.WriteLine("a"), Throws.Nothing);
            Assert.That(() => logger.DumpMethod(new CompiledMethod("a")), Throws.Nothing);
        }

        [Test]
        public void Null_pattern_logs_error_and_matches_nothing()
        {
            var writer = new StringWriter();
            var logger = new DebugLogger(writer, null);

            Assert.That(logger.ShouldLog("a"), Is.False);
            Assert.That(writer.ToString(), Is.Not.Empty);

            logger.WriteLine("!!!");
            Assert.That(writer.ToString(), Does.Contain("!!!"));
        }

        [Test]
        public void Writing_headers_and_text_succeeds()
        {
            var writer = new StringWriter();
            var logger = new DebugLogger(writer, "*");

            logger.WriteHeader("This is a header");
            logger.WriteLine("This is a line");

            Assert.That(writer.ToString(), Is.EqualTo(@"

## This is a header

This is a line
"));
        }

        [Test]
        public void DumpMethod_does_not_dump_non_matching_method()
        {
            var writer = new StringWriter();
            var logger = new DebugLogger(writer, "Namespace::");
            var method = new CompiledMethod("other::method");

            logger.DumpMethod(method);

            Assert.That(writer.ToString(), Is.Empty);
        }

        [Test]
        public void DumpMethod_dumps_matching_method_without_body()
        {
            var writer = new StringWriter();
            var logger = new DebugLogger(writer, "*");
            var method = new CompiledMethod("Namespace::Method");

            logger.DumpMethod(method);

            Assert.That(writer.ToString(), Does.Contain("; Namespace::Method"));
            Assert.That(writer.ToString(), Does.Contain("; (Method has no body)"));
        }

        // DumpMethod with body is tested in CompilerDriverTests because a method body is hard to mock
    }
}

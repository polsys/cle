using System.IO;
using NUnit.Framework;

namespace Cle.Frontend.UnitTests
{
    public class CommandLineParserTests
    {
        [Test]
        public void Version_is_displayed_when_version_flag_is_passed()
        {
            var output = new StringWriter();

            // The CommandLineParser library (not same as the class under test)
            // prints "testhost m.n.o" when running the test suite...
            Assert.That(CommandLineParser.TryParseArguments(new[] { "--version" }, output, out var _), Is.False);
            Assert.That(output.ToString(), Does.Contain("."));
        }

        [Test]
        public void Help_is_displayed_when_requested()
        {
            var output = new StringWriter();
            
            Assert.That(CommandLineParser.TryParseArguments(new[] { "--help" }, output, out var _), Is.False);
            Assert.That(output.ToString(), Does.Contain("--help"));
        }

        [Test]
        public void Dump_regex_is_parsed()
        {
            var output = new StringWriter();
            
            // The real command line could be wrapped in quotes, but that is already handled when Main gets the args.
            Assert.That(CommandLineParser.TryParseArguments(new[] { "--dump", "\\d+" }, output, out var options),
                Is.True);
            Assert.That(options, Is.Not.Null);
            Assert.That(options.DebugPattern, Is.EqualTo("\\d+"));
            Assert.That(options.DebugOutput, Is.True);
        }

        [Test]
        public void Main_module_is_current_directory_by_default()
        {
            var output = new StringWriter();
            
            Assert.That(CommandLineParser.TryParseArguments(new string[] { }, output, out var options),
                Is.True);
            Assert.That(options, Is.Not.Null);
            Assert.That(options.MainModule, Is.EqualTo("."));
        }

        [Test]
        public void Main_module_can_be_overridden()
        {
            var output = new StringWriter();
            
            Assert.That(CommandLineParser.TryParseArguments(new[] { "MainModule" }, output, out var options),
                Is.True);
            Assert.That(options, Is.Not.Null);
            Assert.That(options.MainModule, Is.EqualTo("MainModule"));
        }

        [Test]
        public void Main_module_can_not_be_specified_twice()
        {
            var output = new StringWriter();
            
            Assert.That(CommandLineParser.TryParseArguments(new[] { "Main1", "Main2" }, output, out var options),
                Is.False);
            Assert.That(options, Is.Null);
        }
    }
}

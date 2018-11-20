using NUnit.Framework;

namespace Cle.SemanticAnalysis.UnitTests.MethodCompilerTests
{
    public class ReturnTests : MethodCompilerTestBase
    {
        [Test]
        public void Bool_constant_returning_method_compiled_successfully()
        {
            const string source = @"namespace Test;
public bool ReturnTrue() { return true; }";
            var compiledMethod = TryCompileSingleMethod(source, out var diagnostics);

            Assert.That(compiledMethod, Is.Not.Null);
            Assert.That(diagnostics.Diagnostics, Is.Empty);
            
            AssertDisassembly(compiledMethod, @"
; #0   bool = true
BB_0:
    Return #0");
        }

        // TODO: Int32 return
        // TODO: Void return
        // TODO: Return type validation
        // TODO: Return guarantee and dead code warning (in separate test class)
        // TODO: Int32 expression return
    }
}

using System.IO;
using NUnit.Framework;

namespace Cle.IntegrationTests
{
    public class CodeGenBringUpTests
    {
        private const string BaseDirectory = "TestCases/CodeGenBringUp";
        private const int ExecutionTimeOutMilliseconds = 100;

        [TestCase("IntEquality", 100)]
        [TestCase("ReturnInt", 42)]
        [TestCase("SimpleForwardPhi", 50)]
        public void CodeGenBringUp(string testCase, int expectedReturnCode)
        {
            new TestRunner(Path.Combine(BaseDirectory, testCase))
                .WithDisassembly()
                .CompileInProcess()
                .VerifyCompilationSucceeded()
                .Run(ExecutionTimeOutMilliseconds)
                .VerifyReturnCode(expectedReturnCode);
        }
    }
}

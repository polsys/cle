using System.IO;
using NUnit.Framework;

namespace Cle.IntegrationTests
{
    public class CodeGenBringUpTests
    {
        private const string BaseDirectory = "TestCases/CodeGenBringUp";
        private const int ExecutionTimeOutMilliseconds = 1000;

        [TestCase("FunctionCallWithBoolParamAndResult", 100)]
        [TestCase("FunctionCallWithTwoInt32Params", 15)]
        [TestCase("FunctionCallWithVoid", 100)]
        [TestCase("IntEquality", 100)]
        [TestCase("ReturnInt", 42)]
        [TestCase("ReturnParameter", 100)]
        [TestCase("SimpleForwardPhi", 50)]
        [TestCase("SimpleWhileLoop", 45)]
        [TestCase("SimpleWhileLoop2", 36)]
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

﻿using System.IO;
using NUnit.Framework;

namespace Cle.IntegrationTests
{
    public class FunctionalTests
    {
        private const string BaseDirectory = "TestCases/Functional";
        private const int ExecutionTimeOutMilliseconds = 1000;

        [TestCase("Binomial", 210)]
        [TestCase("DeepLoop", 210)]
        [TestCase("CollatzOnCollatz", 20)]
        [TestCase("IterativeFibonacci", 55)]
        [TestCase("RecursiveFibonacci", 55)]
        [TestCase("SumOfMultiples", 233168)]
        public void Functional(string testCase, int expectedReturnCode)
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

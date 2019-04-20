using System;
using System.IO;
using Cle.CodeGeneration.Lir;
using NUnit.Framework;

namespace Cle.CodeGeneration.UnitTests.Lowering
{
    internal class LoweringTestBase
    {
        protected static void AssertDump<TRegister>(LowMethod<TRegister> method, string expected)
            where TRegister : struct, Enum
        {
            var dumpWriter = new StringWriter();
            method.Dump(dumpWriter, true);

            Assert.That(dumpWriter.ToString().Replace("\r\n", "\n").Trim(), 
                Is.EqualTo(expected.Replace("\r\n", "\n").Trim()));
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;

namespace Cle.CodeGeneration.Lir
{
    /// <summary>
    /// Represents a method in Lowered Intermediate Representation, a form that is closer to the
    /// target assembly language than the general IR.
    ///
    /// This class offers few safety guarantees as it is internal to the code generation.
    /// </summary>
    internal class LowMethod<TRegister>
        where TRegister : struct, Enum
    {
        public readonly List<LowLocal<TRegister>> Locals;

        public readonly List<LowBlock> Blocks;

        /// <summary>
        /// Gets or sets whether this method does not call any other methods.
        /// By default <c>true</c>.
        /// </summary>
        public bool IsLeafMethod { get; set; }

        public LowMethod()
            : this(new List<LowLocal<TRegister>>(), new List<LowBlock>(), true)
        {
        }

        public LowMethod(List<LowLocal<TRegister>> locals, List<LowBlock> blocks, bool isLeafMethod)
        {
            Locals = locals;
            Blocks = blocks;
            IsLeafMethod = isLeafMethod;
        }

        /// <summary>
        /// Prints a debugging representation of this method to the given writer.
        /// </summary>
        /// <param name="writer">A writer instance.</param>
        /// <param name="printLocals">If true, the locals and their required locations will be printed.</param>
        public void Dump(TextWriter writer, bool printLocals)
        {
            if (printLocals)
            {
                for (var i = 0; i < Locals.Count; i++)
                {
                    var local = Locals[i];
                    writer.WriteLine($"; #{i} {local.Type.TypeName} [{local.RequiredLocation}]");
                }
            }

            for (var i = 0; i < Blocks.Count; i++)
            {
                writer.WriteLine("LB_" + i + ":");
                foreach (var instr in Blocks[i].Instructions)
                {
                    writer.WriteLine("    " + instr);
                }
            }
        }
    }
}

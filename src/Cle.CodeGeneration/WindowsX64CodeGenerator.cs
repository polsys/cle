using System;
using System.IO;
using Cle.CodeGeneration.Lir;
using Cle.SemanticAnalysis.IR;
using JetBrains.Annotations;

namespace Cle.CodeGeneration
{
    public sealed class WindowsX64CodeGenerator
    {
        [NotNull] private readonly PortableExecutableWriter _peWriter;

        /// <summary>
        /// Creates a code generator instance with the specified output stream and optional disassembly stream.
        /// </summary>
        /// <param name="outputStream">
        /// Stream for the native code output.
        /// The stream must be writable, seekable and initially empty.
        /// The object lifetime is managed by the caller.
        /// </param>
        /// <param name="disassemblyWriter">
        /// Optional text writer for method disassembly.
        /// </param>
        public WindowsX64CodeGenerator([NotNull] Stream outputStream, [CanBeNull] TextWriter disassemblyWriter)
        {
            _peWriter = new PortableExecutableWriter(outputStream, disassemblyWriter);
        }

        /// <summary>
        /// Completes the executable file.
        /// </summary>
        public void FinalizeFile()
        {
            _peWriter.FinalizeFile();
        }

        /// <summary>
        /// Emits native code for the given method.
        /// </summary>
        /// <param name="method">A compiled method in SSA form.</param>
        /// <param name="methodIndex">The compiler internal index for the method.</param>
        /// <param name="isEntryPoint">If true, this method is marked as the executable entry point.</param>
        public void EmitMethod([NotNull] CompiledMethod method, int methodIndex, bool isEntryPoint)
        {
            _peWriter.StartNewMethod(methodIndex, method.FullName);
            if (isEntryPoint)
            {
                // TODO: Emit a compiler-generated entry point that calls the user-defined entry point
                _peWriter.MarkEntryPoint();
            }

            // TODO: Lower the IR to a low-level form
            var loweredMethod = LoweringX64.Lower(method);

            // TODO: Debug log the lowering
            // TODO: Perform LIR optimization (e.g. peephole)

            // TODO: Allocate registers for locals (with special casing for parameters and special values)
            // For now, just do a best "effort" that happens to work with some methods
            for (var i = 0; i < loweredMethod.Locals.Count; i++)
            {
                if (!loweredMethod.Locals[i].Location.IsRegister)
                    loweredMethod.Locals[i].Location = new StorageLocation<X64Register>((X64Register)(i + 1));
            }

            // TODO: Emit the lowered IR
            for (var i = 0; i < loweredMethod.Blocks.Count; i++)
            {
                _peWriter.Emitter.WriteBlockLabel(i);
                EmitBlock(loweredMethod.Blocks[i], loweredMethod);
            }
        }

        private void EmitBlock(LowBlock block, LowMethod<X64Register> method)
        {
            foreach (var inst in block.Instructions)
            {
                switch (inst.Op)
                {
                    case LowOp.LoadInt:
                        _peWriter.Emitter.EmitLoad(method.Locals[inst.Dest].Location, inst.Data);
                        break;
                    case LowOp.Move:
                        var sourceLocation = method.Locals[inst.Left].Location;
                        var destLocation = method.Locals[inst.Dest].Location;

                        if (sourceLocation != destLocation)
                        {
                            _peWriter.Emitter.EmitMov(destLocation, sourceLocation);
                        }
                        break;
                    case LowOp.Return:
                        _peWriter.Emitter.EmitRet();
                        break; // TODO: Could be return?
                    default:
                        throw new NotImplementedException("Unimplemented LIR opcode");
                }
            }
        }
    }
}

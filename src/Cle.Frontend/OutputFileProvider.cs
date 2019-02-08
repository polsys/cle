using System;
using System.IO;
using Cle.Compiler;
using JetBrains.Annotations;

namespace Cle.Frontend
{
    internal class OutputFileProvider : IOutputFileProvider, IDisposable
    {
        [NotNull] private readonly string _baseDirectory;
        [NotNull] private readonly string _outputName;

        [CanBeNull] private TextWriter _debugWriter;
        [CanBeNull] private TextWriter _disassemblyWriter;
        [CanBeNull] private FileStream _executableStream;

        // TODO: Multi-platform support
        private const string PlatformName = "windows-x64";
        private const string ExecutableExtension = ".exe";
        private const string DisassemblyExtension = ".asm";

        public OutputFileProvider([NotNull] string baseDirectory, [NotNull] string outputName)
        {
            _baseDirectory = baseDirectory;
            _outputName = outputName;
        }

        public void Dispose()
        {
            _debugWriter?.Dispose();
            _disassemblyWriter?.Dispose();
            _executableStream?.Dispose();
        }

        public TextWriter GetDebugFileWriter()
        {
            if (_debugWriter is null)
            {
                try
                {
                    _debugWriter = File.CreateText(Path.Combine(_baseDirectory, "cle-dump.txt"));
                }
                catch (IOException)
                {
                    return null;
                }
            }

            return _debugWriter;
        }

        public Stream GetExecutableStream()
        {
            if (_executableStream is null)
            {
                try
                {
                    var outputFile = Path.Combine(GetAndCreateOutputDirectory(), 
                        Path.ChangeExtension(_outputName, ExecutableExtension));
                    _executableStream = File.Create(outputFile);
                }
                catch (IOException)
                {
                    return null;
                }
            }

            return _executableStream;
        }

        public TextWriter GetDisassemblyWriter()
        {
            if (_disassemblyWriter is null)
            {
                try
                {
                    var outputFile = Path.Combine(GetAndCreateOutputDirectory(), 
                        Path.ChangeExtension(_outputName, DisassemblyExtension));
                    _disassemblyWriter = File.CreateText(outputFile);
                }
                catch (IOException)
                {
                    return null;
                }
            }

            return _disassemblyWriter;
        }

        private string GetAndCreateOutputDirectory()
        {
            var outputFolder = Path.Combine(_baseDirectory, "_bin", PlatformName);
            Directory.CreateDirectory(outputFolder);

            return outputFolder;
        }
    }
}

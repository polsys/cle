using System;
using System.IO;
using Cle.Compiler;
using JetBrains.Annotations;

namespace Cle.Frontend
{
    internal class OutputFileProvider : IOutputFileProvider, IDisposable
    {
        [NotNull] private readonly string _baseDirectory;
        [CanBeNull] private TextWriter _debugWriter;

        public OutputFileProvider([NotNull] string baseDirectory)
        {
            _baseDirectory = baseDirectory;
        }

        public void Dispose()
        {
            _debugWriter?.Dispose();
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
    }
}

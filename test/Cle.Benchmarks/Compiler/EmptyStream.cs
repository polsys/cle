using System;
using System.IO;

namespace Cle.Benchmarks.Compiler
{
    /// <summary>
    /// Compared to <see cref="Stream.Null"/>, this stream updates its position properly.
    /// </summary>
    internal class EmptyStream : Stream
    {
        public override bool CanRead => true;

        public override bool CanSeek => true;

        public override bool CanWrite => true;

        public override long Length => _length;

        // No error checking, anywhere!
        public override long Position { get => _position; set => _position = value; }

        private long _length;
        private long _position;

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin:
                    _position = offset;
                    break;
                case SeekOrigin.Current:
                    _position += offset;
                    break;
                case SeekOrigin.End:
                    _position = _length - offset;
                    break;
            }

            return _position;
        }

        public override void SetLength(long value)
        {
            _length = value;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            _position += count;
            _length = Math.Max(_length, _position);
        }
    }
}

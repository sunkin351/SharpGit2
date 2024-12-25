using static SharpGit2.GitNativeApi;

namespace SharpGit2
{
    public unsafe class GitWriteStream : Stream
    {
        protected internal Native.GitWriteStream* _stream;
        private bool _ownsStream;

        protected internal GitWriteStream(Native.GitWriteStream* stream, bool ownsStream)
        {
            _stream = stream;
            _ownsStream = ownsStream;
        }

        ~GitWriteStream()
        {
            this.Dispose(false);
        }

        public override bool CanRead => false;

        public override bool CanSeek => false;

        public override bool CanWrite => _stream != null;

        public override long Length => throw new NotSupportedException();

        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        protected override void Dispose(bool disposing)
        {
            if (_stream is not null)
            {
                if (_ownsStream)
                    _stream->Free(_stream);
                
                _stream = null;
            }
        }

        public override void Flush()
        {
            ObjectDisposedException.ThrowIf(_stream == null, this);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            this.Write(buffer.AsSpan(offset, count));
        }

        public override void Write(ReadOnlySpan<byte> buffer)
        {
            var stream = _stream;

            ObjectDisposedException.ThrowIf(stream == null, this);

            fixed (byte* _buffer = buffer)
            {
                Git2.ThrowIfError((GitError)stream->Write(stream, _buffer, (nuint)buffer.Length));
            }
        }
    }

    public sealed unsafe class GitBlobWriteStream : GitWriteStream
    {
        internal GitBlobWriteStream(Native.GitWriteStream* stream) : base(stream, true)
        {
        }

        public GitObjectID Commit()
        {
            GitObjectID result = default;
            Git2.ThrowIfError(git_blob_create_from_stream_commit(&result, _stream));

            _stream = null;
            return result;
        }
    }
}

namespace SharpGit2.Native
{
    public unsafe struct GitWriteStream
    {
        public delegate* unmanaged[Cdecl]<GitWriteStream*, byte*, nuint, int> Write;
        public delegate* unmanaged[Cdecl]<GitWriteStream*, int> Close;
        public delegate* unmanaged[Cdecl]<GitWriteStream*, void> Free;
    }
}

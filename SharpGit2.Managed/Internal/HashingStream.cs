using System.Security.Cryptography;

namespace SharpGit2.Managed.Internal;

internal sealed class HashingStream : Stream
{
    private readonly Stream _stream;
    private readonly IncrementalHash _hash;

    public HashingStream(Stream stream, IncrementalHash hashctx)
    {
        _stream = stream;
        _hash = hashctx;
    }

    public override bool CanRead => false;

    public override bool CanSeek => false;

    public override bool CanWrite => true;

    public override long Length => throw new NotSupportedException();

    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public override void Flush()
    {
        _stream.Flush();
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
        _stream.Write(buffer);
        _hash.AppendData(buffer);
    }

    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled(cancellationToken);

        var task = _stream.WriteAsync(buffer, offset, count, cancellationToken);

        _hash.AppendData(buffer, offset, count);

        return task;
    }

    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
            return ValueTask.FromCanceled(cancellationToken);

        var task = _stream.WriteAsync(buffer, cancellationToken);

        _hash.AppendData(buffer.Span);

        return task;
    }
}

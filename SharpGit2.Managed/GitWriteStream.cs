namespace SharpGit2.Managed;

/// <summary>
/// Write only stream, to mimic libgit2's `git_writestream` which only allowed writing.
/// </summary>
public abstract class GitWriteStream : Stream
{
    protected bool _disposed;
    
    public sealed override bool CanRead => false;
    public sealed override bool CanSeek => false;
    public sealed override bool CanWrite => !_disposed;

    public sealed override long Length => throw new NotSupportedException();

    public sealed override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }
    
    public sealed override int Read(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException();
    }

    public sealed override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotSupportedException();
    }

    public sealed override void SetLength(long value)
    {
        throw new NotSupportedException();
    }
}
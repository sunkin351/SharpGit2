using System.Buffers;
using CommunityToolkit.HighPerformance.Buffers;

namespace SharpGit2.Managed.Filter;

public sealed class GitFilterBufferedStream : GitWriteStream
{
    private readonly GitFilterSource Source;
    private readonly Stream Next;
    private readonly ArrayPoolBufferWriter<byte> InputBuffer = new();
    private readonly Func<ReadOnlySpan<byte>, IBufferWriter<byte>, bool> ApplyOperation;
    private bool _disposed;
    
    public GitFilterBufferedStream(GitFilterSource source, Stream next, Func<ReadOnlySpan<byte>, IBufferWriter<byte>, bool> applyOperation)
    {
        Source = source;
        Next = next;
        ApplyOperation = applyOperation;
    }
    
    public override void Flush()
    {
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        this.Write(buffer.AsSpan(offset, count));
    }

    public override void Write(ReadOnlySpan<byte> buffer)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (buffer.IsEmpty)
            return;
        
        var writer = this.InputBuffer;
        var span = writer.GetSpan(buffer.Length);
        buffer.CopyTo(span);
        writer.Advance(buffer.Length);
    }

    protected override void Dispose(bool disposing)
    {
        if (_disposed || !disposing)
            return;
        
        _disposed = true;

        try
        {
            var inputBuffer = this.InputBuffer.WrittenSpan;

            using var outputBuffer = new ArrayPoolBufferWriter<byte>(inputBuffer.Length);

            if (this.ApplyOperation(inputBuffer, outputBuffer))
            {
                this.Next.Write(outputBuffer.WrittenSpan);
            }
            else
            {
                // Pass Through, 
                this.Next.Write(inputBuffer);
            }
        }
        finally
        {
            this.InputBuffer.Dispose();
            this.Next.Dispose();
        }
    }
}
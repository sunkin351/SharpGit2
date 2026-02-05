using System.Buffers;
using System.Diagnostics;

namespace SharpGit2.Managed.Internal;

internal sealed class MemoryTextReader(ReadOnlyMemory<char> buffer) : TextReader
{
    private readonly ReadOnlyMemory<char> _buffer = buffer;
    private int _position;

    public override int Read(char[] buffer, int index, int count)
    {
        return this.Read(buffer.AsSpan(index, count));
    }

    public override int Read(Span<char> buffer)
    {
        int position = _position;
        int internalBufferLen = _buffer.Length;

        if ((uint)position >= (uint)internalBufferLen)
            return 0;

        int count = Math.Min(internalBufferLen - position, buffer.Length);

        _buffer.Span.Slice(position, count).CopyTo(buffer);

        _position = position + count;

        return count;
    }
}

internal sealed class SequenceTextReader(ReadOnlySequence<char> backingSequence) : TextReader
{
    private readonly ReadOnlySequence<char> _sequence = backingSequence;
    private SequencePosition _position = backingSequence.Start;

    public override int Read(char[] buffer, int index, int count)
    {
        return this.Read(buffer.AsSpan(index, count));
    }

    public override int Read(Span<char> buffer)
    {
        if (_position.Equals(_sequence.End))
        {
            return 0;
        }

        int copied = 0;
        while (_sequence.TryGet(ref _position, out var memory, false))
        {
            Debug.Assert(memory.Length > 0);

            var toCopy = Math.Min(buffer.Length, memory.Length);

            memory.Span.Slice(0, toCopy).CopyTo(buffer);

            buffer = buffer.Slice(toCopy);
            _position = _sequence.GetPosition(toCopy, _position);
            copied += toCopy;

            if (buffer.IsEmpty || _position.Equals(_sequence.End))
                break;
        }

        return copied;
    }

    public override int ReadBlock(char[] buffer, int index, int count)
    {
        return this.ReadBlock(buffer.AsSpan(index, count));
    }

    public override int ReadBlock(Span<char> buffer)
    {
        return this.Read(buffer); // This implementation guarentees the buffer is full or the source is empty.
    }
}
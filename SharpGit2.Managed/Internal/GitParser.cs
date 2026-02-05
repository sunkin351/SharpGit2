using System.Diagnostics;

namespace SharpGit2.Managed.Internal;

internal class GitParser : IDisposable
{
    private TextReader? _contentReader;
    private bool _ownsReader;

    private char[] _buffer = new char[1024]; // Buffer containing buffered text from the stream

    protected ReadOnlySpan<char> RawBuffer => _buffer;

    /// <summary>
    /// Beginning index of the current line into the buffer, also the start of the presently buffered data
    /// </summary>
    protected int LineStart { get; private set; }
    /// <summary>
    /// Length of the current line
    /// </summary>
    protected int LineLength { get; private set; }
    /// <summary>
    /// Length of the buffered data, starting from <see cref="LineStart"/>
    /// </summary>
    protected int DataLength { get; private set; }
    /// <summary>
    /// 1-based index of the current line (0 means no data has been read yet)
    /// </summary>
    protected int LineNumber { get; private set; }
    /// <summary>
    /// We've reached the end of the stream, but there may still be buffered data
    /// </summary>
    protected bool FinalLine { get; private set; }

    protected int LinePosition { get; private set; }

    public GitParser(TextReader reader)
    {
        this.Reset(reader, true);
    }

    protected void Reset(TextReader reader, bool ownsReader)
    {
        if (_ownsReader && _contentReader is { } lastReader)
            lastReader.Dispose();

        _contentReader = reader;
        _ownsReader = ownsReader;
        LineStart = 0;
        LineLength = 0;
        DataLength = 0;
        LineNumber = 0;
        FinalLine = false;
        LinePosition = 0;
    }

    public void Dispose()
    {
        if (_ownsReader)
            _contentReader?.Dispose();

        _contentReader = null;
        _ownsReader = false;
    }

    protected ReadOnlySpan<char> GetCurrentLine()
    {
        if (LineLength == 0)
        {
            if (!this.AdvanceToNextLine())
                return default;
        }

        return _buffer.AsSpan(LineStart, LineLength);
    }

    protected bool AdvanceToNextLine()
    {
        if (this.FinalLine)
            return false;

        int currentLineLen = this.LineLength;

        this.LineStart += currentLineLen;
        this.DataLength -= currentLineLen;

        while (true)
        {
            int newLength = _buffer.AsSpan(this.LineStart, this.DataLength).IndexOf('\n');

            if (newLength >= 0)
            {
                this.LineLength = newLength + 1;
                this.LineNumber += 1;
                this.LinePosition = 0;

                return true;
            }

            if (this.FinalLine)
            {
                this.LineLength = DataLength;
                this.LineNumber += 1;
                this.LinePosition = 0;

                return DataLength != 0;
            }

            if (!this.ReadMore())
            {
                this.FinalLine = true;
            }
        }
    }

    private bool ReadMore()
    {
        var reader = _contentReader ?? throw new InvalidOperationException();
        int dataLen = DataLength;

        if (dataLen > 0)
        {
            if (LineStart > 0)
            {
                // Shift the remaining data to the beginning of the buffer
                Array.Copy(_buffer, LineStart, _buffer, 0, dataLen);
                LineStart = 0;
            }
            else if (dataLen == _buffer.Length)
            {
                // Resize the buffer if there's no more room for new data
                var newBuffer = new char[checked(dataLen * 2)];

                Array.Copy(_buffer, newBuffer, dataLen); // Assume the remaining data has already been shifted to the beginning of the buffer

                _buffer = newBuffer;
            }
        }

        Span<char> buffer = _buffer.AsSpan(dataLen);

        int readChars = reader.Read(buffer);

        if (readChars == 0)
        {
            return false;
        }

        DataLength += readChars;
        return true;
    }
    
    protected ReadOnlySpan<char> GetRemainingLine()
    {
        return this.GetCurrentLine().Slice(this.LinePosition);
    }

    protected bool AdvanceExpected(ReadOnlySpan<char> expected)
    {
        var currentLine = this.GetRemainingLine();

        if (currentLine.StartsWith(expected))
        {
            this.LinePosition += expected.Length;
            return true;
        }

        return false;
    }

    protected bool AdvanceCount(int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        Debug.Assert(this.LinePosition >= 0);

        int nextPos = checked(count + this.LinePosition);
        if (nextPos <= this.LineLength)
        {
            this.LinePosition = nextPos;
            return true;
        }

        return false;
    }

    protected void AdvanceWhitespace()
    {
        do
        {
            var currentLine = this.GetRemainingLine();

            for (int i = 0; i < currentLine.Length; ++i)
            {
                if (!char.IsWhiteSpace(currentLine[i]))
                {
                    this.LinePosition += i;
                    return;
                }
            }
        }
        while (this.AdvanceToNextLine()); // the entire rest of the line is whitespace, move to the next
    }

    protected bool AdvanceDigit(int numberBase, out long result)
    {
        var content = this.GetRemainingLine();

        if (Utilities.TryParseLong(content, out result, out int consumed, numberBase))
        {
            bool success = this.AdvanceCount(consumed);
            Debug.Assert(success);
            return true;
        }

        result = default;
        return false;
    }

    protected bool AdvanceObjectId(GitObjectIDType type, out GitObjectID result)
    {
        int hexSize = type.HashSize * 2; // Extension property!

        var remaining = this.GetRemainingLine();

        if (remaining.Length >= hexSize && GitObjectID.TryParsePrefix(remaining, out result, out ushort nibbleLen) && nibbleLen == hexSize)
        {
            bool success = this.AdvanceCount(hexSize);
            Debug.Assert(success);

            return true;
        }

        result = default;
        return false;
    }

    protected bool AdvanceEOL()
    {
        var line = this.GetRemainingLine();

        if (line.Length == 0 || line.SequenceEqual("\n"))
        {
            this.AdvanceToNextLine();
            return true;
        }

        return false;
    }

    protected bool TryPeek(bool skipWhitespace, out char ch)
    {
        var remainingLine = this.GetRemainingLine();

        if (remainingLine.Length > 0)
        {
            if (skipWhitespace && char.IsWhiteSpace(remainingLine[0]))
            {
                int position = 1;
                while (position < remainingLine.Length)
                {
                    if (!char.IsWhiteSpace(remainingLine[position]))
                        break;

                    position += 1;
                }

                if ((uint)position < (uint)remainingLine.Length)
                {
                    ch = remainingLine[position];
                    return true;
                }
            }
            else
            {
                ch = remainingLine[0];
                return true;
            }
        }

        ch = default;
        return false;
    }
}

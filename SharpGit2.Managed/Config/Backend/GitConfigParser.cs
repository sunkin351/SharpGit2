using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;

using CommunityToolkit.HighPerformance.Buffers;
using SharpGit2.Managed.Internal;

namespace SharpGit2.Managed.Config.Backend;

internal sealed class GitConfigParser : GitParser
{
    internal const string ConfigEscapes = "ntb\"\\";
    internal const string ConfigEscaped = "\n\t\b\"\\";

    public interface ICallbacks
    {
        void OnSection(GitConfigParser parser, string currentSection, ReadOnlySpan<char> line);

        void OnVariable(GitConfigParser parser,
            string? currentSection,
            string variableName,
            string? variableValue,
            ReadOnlySpan<char> line);

        void OnComment(GitConfigParser parser, ReadOnlySpan<char> line);

        void OnEndOfFile(GitConfigParser parser, string? currentSection);
    }

    public string Path { get; private set; }

    public GitConfigParser(TextReader reader, string path) : base(reader)
    {
        this.Path = path;
    }

    public void Parse(ICallbacks callbacks)
    {
        string? currentSection = null;

        while (this.AdvanceToNextLine())
        {
        restart:
            ReadOnlySpan<char> currentLine = this.GetRemainingLine();
            int line_len = currentLine.Length;

            if (!this.TryPeek(true, out char c)
                && !this.TryPeek(false, out c))
            {
                continue;
            }

            switch (c)
            {
                case '[': // section header, new section begins
                    int consumed = this.ParseSectionHeader(out currentSection);

                    Debug.Assert(consumed > 2); // The [ and ] along with at least one name character

                    this.AdvanceCount(consumed);

                    callbacks.OnSection(this, currentSection, currentLine);

                    if (this.TryPeek(true, out c))
                        goto restart;

                    break;

                case '\n':
                case '\r':
                case ' ':
                case '\t':
                case ';':
                case '#':
                    callbacks.OnComment(this, currentLine);
                    break;
                default: // assume variable declaration
                {
                    var (name, value) = this.ParseVariable(ref line_len);

                    callbacks.OnVariable(this, currentSection, name, value, currentLine);

                    break;
                }
            }
        }

        callbacks.OnEndOfFile(this, currentSection);
    }

    private static bool IsKeyCharacter(char c)
    {
        return char.IsAsciiLetterOrDigit(c) | c == '-';
    }

    private static int StripComments(ref ReadOnlySpan<char> line, int in_quotes)
    {
        int quote_count = in_quotes, backslash_count = 0;

        ReadOnlySpan<char> ptr = line;
        int i = 0;

        while (i < ptr.Length)
        {
            if (ptr[i] == '\"')
            {
                if (i == 0 || ((uint)i - 1 < (uint)ptr.Length && ptr[i - 1] != '\\'))
                {
                    quote_count += 1;
                }
            }

            if (ptr[i] is ';' or '#' && (quote_count % 2) == 0 && (backslash_count % 2) == 0)
            {
                ptr = ptr[..i];
                break;
            }

            backslash_count = ptr[i] == '\\' ? backslash_count + 1 : 0;

            i += 1;
        }

        line = ptr.TrimEnd();
        return quote_count;
    }

    private int ParseSubsectionHeader(ReadOnlySpan<char> line, int pos, StringBuilder name)
    {
        // parse_subsection_header()

        while ((uint)pos < (uint)line.Length && char.IsWhiteSpace(line[pos]))
            pos += 1;

        if ((uint)pos >= (uint)line.Length || line[pos] != '\"')
            this.ThrowError(0, "Missing quotation marks in section header");

        int firstQuote = pos;
        int lastQuote = line.LastIndexOf('\"');

        if (firstQuote == lastQuote)
            this.ThrowError(0, "Missing closing quotation mark in section header");

        int quoteLen = lastQuote - (firstQuote + 1);

        var buffer = name.Append('.');

        pos += 1;

        do
        {
            char c = line[pos];

            if (c == '\"')
            {
                break;
            }
            else if (c == '\\')
            {
                pos += 1;
                if ((uint)pos >= (uint)line.Length)
                    this.ThrowError(pos, "Unexpected end-of-file in section header");

                c = line[pos];
            }

            buffer.Append(c);
            pos += 1;
        }
        while ((uint)pos < (uint)line.Length);

        if (!line[pos..].StartsWith("\"]"))
        {
            this.ThrowError(pos, "Unexpected text after closing quotes");
        }

        return pos + 2;
    }

    private int ParseSectionHeader(out string sectionName)
    {
        this.AdvanceWhitespace();

        var line = this.GetRemainingLine();

        int nameEnd = line.LastIndexOf(']');
        if (nameEnd < 0)
        {
            this.ThrowError(0, "Missing ']' in section header");
        }

        Debug.Assert(line.StartsWith('['));

        var name = line[..(nameEnd + 1)];
        var builder = new StringBuilder(name.Length);
        int i = 1;

        do
        {
            char c = name[i];
            if (char.IsWhiteSpace(c))
            {
                int result = ParseSubsectionHeader(name, i, builder);
                sectionName = builder.ToString();
                return result;
            }

            if (!IsKeyCharacter(c) && c != '.')
            {
                this.ThrowError(this.LinePosition + i, "Unexpected character in header");
            }

            builder.Append(c);
            i += 1;
        }
        while ((uint)i < (uint)name.Length && name[i] != ']');

        sectionName = builder.ToString();

        return i + 1;
    }

    private void UnescapeLine(ReadOnlySpan<char> source, ref int quote_count, IBufferWriter<char> bufferWriter, out bool is_multi)
    {
        is_multi = false;

        Span<char> buffer = bufferWriter.GetSpan(source.Length);

        Debug.Assert(buffer.Length >= source.Length);

        int outputIdx = 0;

        for (int sourceIdx = 0; sourceIdx < source.Length; ++sourceIdx)
        {
            if (source[sourceIdx] != '\\')
            {
                if (source[sourceIdx] != '\"')
                {
                    buffer[outputIdx++] = source[sourceIdx];
                    continue;
                }
                else
                {
                    quote_count += 1;
                    continue;
                }
            }

            sourceIdx += 1;
            if ((uint)sourceIdx >= (uint)source.Length)
            {
                is_multi = true;
                break;
            }

            int idx = ConfigEscapes.IndexOf(source[sourceIdx]);
            if (idx < 0)
            {
                this.ThrowError(0, $"Invalid escape sequence \\{source[sourceIdx]}");
            }

            Debug.Assert(ConfigEscapes.Length == ConfigEscaped.Length);
            buffer[outputIdx++] = ConfigEscaped[idx];
        }

        bufferWriter.Advance(outputIdx);
    }

    private void ParseMultilineVariable(IBufferWriter<char> bufferWriter, int in_quotes, ref int lineLen)
    {
        Debug.Assert(in_quotes >= 0);
        Debug.Assert(lineLen >= 0);

        bool multline = true;
        while (multline)
        {
            if (!this.AdvanceToNextLine())
                break; // EOF

            var line = this.GetCurrentLine();

            lineLen = checked(line.Length + lineLen);

            int quoteCount = StripComments(ref line, in_quotes);
            if (line.IsEmpty)
                continue;

            UnescapeLine(line, ref quoteCount, bufferWriter, out multline);

            in_quotes = quoteCount;
        }
    }

    private static bool IsNameCharacter(char c)
    {
        return char.IsAsciiLetterOrDigit(c) || c == '-';
    }

    private void ParseName(ReadOnlySpan<char> line, out ReadOnlySpan<char> name, out ReadOnlySpan<char> value)
    {
        name = default;
        value = default;

        int name_end = 0;
        while ((uint)name_end < (uint)line.Length && IsNameCharacter(line[name_end]))
        {
            name_end += 1;
        }

        if (name_end == 0)
            this.ThrowError(0, "Invalid configuration key");

        int value_start = name_end;

        while ((uint)value_start < (uint)line.Length && char.IsWhiteSpace(line[value_start]))
        {
            value_start += 1;
        }

        if ((uint)value_start < (uint)line.Length)
        {
            if (line[value_start] == '=')
            {
                value = line.Slice(value_start + 1);
            }
            else
            {
                this.ThrowError(0, "Invalid configuration key");
            }
        }

        name = line.Slice(0, name_end);
    }

    private (string name, string? value) ParseVariable(ref int line_len)
    {
        this.AdvanceWhitespace();

        var line = this.GetRemainingLine();

        int quote_count = StripComments(ref line, 0);

        this.ParseName(line, out var name, out var value_start);

        string? value = null;
        if (!value_start.IsEmpty)
        {
            value_start = value_start.TrimStart();

            using var valueTextBuffer = new ArrayPoolBufferWriter<char>();

            int _throwAway = default;
            this.UnescapeLine(value_start, ref _throwAway, valueTextBuffer, out bool multiline);

            Debug.Assert(quote_count == _throwAway); // theory

            if (multiline)
            {
                ParseMultilineVariable(valueTextBuffer, quote_count % 2, ref line_len);
            }

            value = valueTextBuffer.WrittenSpan.ToString();
        }

        return (name.ToString(), value);
    }

    [DoesNotReturn]
    private void ThrowError(int col, string errorMessage)
    {
        string finalMessage = col != 0
            ? $"Failed to parse config file: {errorMessage} (in {this.Path}:{this.LineNumber}, column {col})"
            : $"Failed to parse config file: {errorMessage} (in {this.Path}:{this.LineNumber})";

        throw new GitConfigParseException(finalMessage);
    }
}

public sealed class GitConfigParseException : Git2Exception
{
    public GitConfigParseException(string message) : base(message)
    {
    }

    public GitConfigParseException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
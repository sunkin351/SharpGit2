using System.Collections.Immutable;
using System.Diagnostics;
using System.Security.Cryptography;

namespace SharpGit2.Managed.Internal;

internal struct GitCommitGraft
{
    public GitObjectID Oid;
    public ImmutableArray<GitObjectID> Parents;
}

internal sealed class GitGrafts
{
    // Keys are commit object ID's, values are arrays of their parents commits
    private readonly Dictionary<GitObjectID, ImmutableArray<GitObjectID>> _commits = new();

    /// <summary>
    /// Type of object ID's
    /// </summary>
    private readonly GitObjectIDType oid_type;

    /// <summary>
    /// File backing the graft. <see langword="null"> if it's an in-memory graft
    /// </summary>
    private string? Path;

    /// <summary>
    /// SHA256 checksum
    /// </summary>
    private readonly byte[] PathChecksum = new byte[SHA256.HashSizeInBytes];

    internal GitGrafts(GitObjectIDType oidType, string? path)
    {
        oid_type = oidType;
        Path = path;
    }

    public static GitGrafts Open(string? path, GitObjectIDType oidType)
    {
        var ret = new GitGrafts(oidType, path);

        ret.Refresh();

        return ret;
    }

    public void Refresh()
    {
        string? path = this.Path;

        if (path is null)
            return;

        Span<byte> newChecksum = stackalloc byte[SHA256.HashSizeInBytes];

        try
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read);

            int written = SHA256.HashData(stream, newChecksum);
            Debug.Assert(written == SHA256.HashSizeInBytes);

            // The hash algorithm used here may be cryptographic in nature, but our use of it is not.
            // Use of `System.Security.Cryptography.CryptographicOperations.FixedTimeEquals()` is unnecessary.
            if (!newChecksum.SequenceEqual(this.PathChecksum))
            {
                stream.Position = 0;

                _commits.Clear();
                this.Parse(stream);

                newChecksum.CopyTo(this.PathChecksum);
            }
        }
        catch (FileNotFoundException)
        {
            _commits.Clear();
        }
    }

    private void Parse(Stream stream)
    {
        using var reader = new StreamReader(stream);

        var parser = new Parser(reader);

        List<GitObjectID> parents = [];

        while (parser.CurrentLine is not null)
        {
            if (!parser.TryAdvanceOID(this.oid_type, out GitObjectID graft_oid))
            {
                throw new InvalidDataException($"Invalid graft OID at line {parser.LineNumber}!");
            }

            while (!parser.TryAdvanceNewLine())
            {
                if (!parser.TryAdvanceExpected(" ") || !parser.TryAdvanceOID(this.oid_type, out var parent))
                {
                    throw new InvalidDataException($"Invalid parent OID at line {parser.LineNumber}!");
                }

                parents.Add(parent);
            }

            this.Add(graft_oid, parents);
            parents.Clear();
        }
    }

    private void Add(GitObjectID oid, IEnumerable<GitObjectID> parents)
    {
        _commits[oid] = [.. parents]; // add or update
    }

    private bool Remove(GitObjectID oid)
    {
        return _commits.Remove(oid);
    }

    public bool TryGetParents(GitObjectID oid, out ImmutableArray<GitObjectID> parents)
    {
        return _commits.TryGetValue(oid, out parents);
    }

    public IEnumerable<GitObjectID> ObjectIDs => _commits.Keys;

    public int Count => _commits.Count;

    private sealed class Parser
    {
        private readonly TextReader _reader;
        public string? CurrentLine { get; private set; }
        public int LineNumber { get; private set; }

        private int _linePos = 0;

        public Parser(TextReader reader)
        {
            _reader = reader ?? TextReader.Null;

            CurrentLine = _reader.ReadLine();
            LineNumber = 1;
        }

        public void AdvanceLine()
        {
            // Naive implementation until proper buffering can be implemented.
            CurrentLine = _reader.ReadLine();
            LineNumber += 1;
            _linePos = 0;
        }

        public void AdvanceCharacters(int count)
        {
            _linePos = Math.Min(_linePos + count, CurrentLine?.Length ?? 0);
        }

        public bool TryAdvanceExpected(ReadOnlySpan<char> expected)
        {
            if (CurrentLine.AsSpan(_linePos).StartsWith(expected))
            {
                AdvanceCharacters(expected.Length);
                return true;
            }

            return false;
        }

        public bool AdvanceWhitespace()
        {
            bool ret = false;

            int pos = _linePos;
            ReadOnlySpan<char> line = CurrentLine;

            while ((uint)pos < (uint)line.Length
                && char.IsWhiteSpace(line[pos]))
            {
                pos += 1;
                ret = true;
            }

            _linePos = pos;
            return ret;
        }

        public bool TryAdvanceNewLine()
        {
            if (_linePos < (CurrentLine?.Length ?? 0))
            {
                return false;
            }

            AdvanceLine();
            return true;
        }

        public long AdvanceDigit(int numberBase)
        {
            AdvanceWhitespace();

            if (Utilities.TryParseLong(CurrentLine.AsSpan(_linePos), out long value, out int consumed, numberBase))
            {
                AdvanceCharacters(consumed);
                return value;
            }

            throw new InvalidDataException("Failed to parse integer!");
        }

        public bool TryAdvanceOID(GitObjectIDType type, out GitObjectID oid)
        {
            int size = type switch
            {
                GitObjectIDType.SHA1 => SHA1.HashSizeInBytes,
#if GIT_EXPERIMENTAL_SHA256
                GitObjectIDType.SHA256 => SHA256.HashSizeInBytes,
#endif
                _ => throw new ArgumentOutOfRangeException(nameof(type))
            };

            // Convert to Nibble length.
            // Each character can only represent a nibble.
            size *= 2;

            ReadOnlySpan<char> line = CurrentLine.AsSpan(_linePos);

            if (line.Length >= size && GitObjectID.TryParsePrefix(line.Slice(0, size), out oid, out ushort nibbleLen) && nibbleLen == size)
            {
                AdvanceCharacters(size);
                return true;
            }

            oid = default;
            return false;
        }

        public bool TryPeek(bool skipWhitespace, out char c)
        {
            ReadOnlySpan<char> line = CurrentLine;
            line = line.Slice(_linePos);

            if (skipWhitespace)
            {
                line = line.TrimStart();
            }

            if (!line.IsEmpty)
            {
                c = line[0];
                return true;
            }

            c = default;
            return false;
        }
    }
}

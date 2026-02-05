using System.Buffers;
using System.Diagnostics;
using System.Runtime.InteropServices;
using CommunityToolkit.HighPerformance.Buffers;

namespace SharpGit2.Managed.Internal;

internal static class PathUtilities
{
    public static readonly StringPool PathStringPool = new();
    public static readonly SearchValues<char> InvalidPathCharacters = SearchValues.Create(Path.GetInvalidPathChars());

    private static readonly SearchValues<char> Win32PathSeparators = SearchValues.Create('/', '\\');

    public static ReadOnlySpan<char> GetDirectoryName(ReadOnlySpan<char> path) => Path.GetDirectoryName(path);

    public static ReadOnlySpan<char> GetBaseName(ReadOnlySpan<char> path) => Path.GetFileName(path);

    public static int GetBaseNameOffset(ReadOnlySpan<char> path)
    {
        Debug.Assert(path.TrimEnd().Length == path.Length);

        return path.Length - GetBaseName(path).Length; // basename is always at the end of the path
    }

    public static int GetRootLength(ReadOnlySpan<char> path) => Path.GetPathRoot(path).Length;

    public static bool IsPathRoot(ReadOnlySpan<char> path)
    {
        if (OperatingSystem.IsWindows())
        {
            return path.Length == 3 && char.IsAsciiLetter(path[0]) && path[1] == ':' && path[2] is '/' or '\\';
        }

        return path.SequenceEqual("/");
    }

    public static bool IsPathDotOrDotDot(ReadOnlySpan<char> name) => name is "." or "..";

    public static bool IsPathAbsolute(ReadOnlySpan<char> path)
    {
        if (OperatingSystem.IsWindows())
        {
            return path.Length >= 3 && char.IsAsciiLetter(path[0]) && path[1] == ':' && path[2] is '/' or '\\';
        }

        return path.StartsWith('/');
    }

    public static void MakePosix(Span<char> buffer)
    {
        buffer.Replace('\\', '/');
    }

    public static void MakePosix(ReadOnlySpan<char> input, Span<char> output)
    {
        Debug.Assert(output.Length >= input.Length);

        input.Replace(output, '\\', '/');
    }

    public static bool IsPathRelative(ReadOnlySpan<char> path)
    {
        return path.StartsWith("./") || path.StartsWith("../");
    }

    public static string PathFromUrl(string url)
    {
        throw new NotImplementedException();
    }

    public static bool EndsInDirectorySeparator(ReadOnlySpan<char> path)
    {
        if (GetRootLength(path) < path.Length)
        {
            return path[^1] == '/' || (OperatingSystem.IsWindows() && path[^1] == '\\');
        }

        return false;
    }

    public static string JoinUnrooted(string path, string? @base, out int root_at)
    {
        ArgumentNullException.ThrowIfNull(path);

        int root = Path.GetPathRoot(path.AsSpan()).Length;

        string result;
        if (@base is not null && root == 0)
        {
            result = Path.Join(@base, path);

            root = @base.Length;
        }
        else
        {
            result = path;

            if (@base is not null)
                PathEqualOrPrefix(@base, path, out root);
        }

        root_at = root;
        return result;
    }

    public enum EqualOrPrefixResult
    {
        NotEqual,
        Equal,
        Prefix
    }

    public static EqualOrPrefixResult PathEqualOrPrefix(string parent, string child, out int prefixLen)
    {
        int p = 0, c = 0;
        bool lastSlash = false;

        while (p < parent.Length && c < child.Length)
        {
            lastSlash = parent[p] == '/';

            if (parent[p++] != child[c++])
                goto not_equal;
        }

        if (p >= parent.Length)
        {
            if (c >= child.Length)
            {
                prefixLen = p;
                return EqualOrPrefixResult.Equal;
            }

            if (child[c] == '/' || lastSlash)
            {
                prefixLen = p - (lastSlash ? 1 : 0);
                return EqualOrPrefixResult.Prefix;
            }
        }

    not_equal:
        prefixLen = 0;
        return EqualOrPrefixResult.NotEqual;
    }

    public static string NormalizePath(string path)
    {
        if (OperatingSystem.IsWindows())
        {
            if (path.Contains('\\'))
            {
                var span = Path.TrimEndingDirectorySeparator(path.AsSpan());

                return string.Create(span.Length, span, (output, input) =>
                {
                    input.Replace(output, '\\', '/'); // normalize all paths to use /'s
                });
            }
        }

        if (Path.EndsInDirectorySeparator(path))
        {
            return Path.TrimEndingDirectorySeparator(path);
        }

        return path;
    }

    public static string? FindDirectory(string directory)
    {
        directory = Path.GetFullPath(directory);

        return Path.GetDirectoryName(directory);
    }

    private ref struct ValueStringBuilder
    {
        private char[]? _array;
        private Span<char> _span;
        private int _pos;

        public ValueStringBuilder(Span<char> initialBuffer)
        {
            _span = initialBuffer;
        }

        public ValueStringBuilder(int initialCapacity)
        {
            _array = ArrayPool<char>.Shared.Rent(initialCapacity);
            _span = _array;
        }

        public void Append(char value)
        {
            EnsureCapacity(_pos + 1);

            _span[_pos++] = value;
        }

        public void Append(ReadOnlySpan<char> span)
        {
            int pos = _pos;

            EnsureCapacity(pos + span.Length);

            span.CopyTo(_span.Slice(pos));

            _pos = pos + span.Length;
        }

        public void AppendWithReplace(ReadOnlySpan<char> span, char oldChar, char newChar)
        {
            int pos = _pos;

            EnsureCapacity(pos + span.Length);

            span.Replace(_span.Slice(pos), oldChar, newChar);

            _pos = pos + span.Length;
        }

        public void NormalizePath(ReadOnlySpan<char> path)
        {
            char[]? pooledArray = null;
            if (!IsPathAbsolute(path))
            {
                string cwd = Environment.CurrentDirectory;

                Debug.Assert(!string.IsNullOrEmpty(cwd));

                pooledArray = ArrayPool<char>.Shared.Rent(cwd.Length + 1 + path.Length);

                bool success = Path.TryJoin(cwd, path, pooledArray, out int written);
                Debug.Assert(success);

                path = pooledArray.AsSpan(0, written);
            }

            try
            {
                int rootLength = GetRootLength(path);

                if (OperatingSystem.IsWindows())
                {

                }
                else
                {



                }
            }
            finally
            {
                if (pooledArray is not null)
                    ArrayPool<char>.Shared.Return(pooledArray);
            }
        }

        public ref char GetPinnableReference()
        {
            // Make into null-terminated string and return reference to the beginning
            EnsureCapacity(_pos + 1);

            _span[_pos] = '\0';

            return ref MemoryMarshal.GetReference(_span);
        }

        public readonly ReadOnlySpan<char> WrittenSpan => _span.Slice(0, _pos);

        private void EnsureCapacity(int capacity)
        {
            Debug.Assert(capacity > 0);

            if (_span.Length < capacity)
            {
                Grow(capacity);
            }
        }

        private void Grow(int newCap)
        {
            char[] array = ArrayPool<char>.Shared.Rent(newCap);

            _span[.._pos].CopyTo(array);

            _span = array;

            var array2 = _array;
            if (array2 is not null)
                ArrayPool<char>.Shared.Return(array2);

            _array = array;
        }

        public void Dispose()
        {
            if (_array is not null)
                ArrayPool<char>.Shared.Return(_array);

            _array = null;
            _span = default;
            _pos = 0;
        }
    }
}

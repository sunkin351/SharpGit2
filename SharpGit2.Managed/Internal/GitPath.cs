using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO.Enumeration;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Versioning;
using System.Text;

using CommunityToolkit.HighPerformance;
using CommunityToolkit.HighPerformance.Buffers;

using Mono.Unix.Native;

using SharpGit2.Managed.Config;

using TerraFX.Interop.Windows;

namespace SharpGit2.Managed.Internal;

internal static class GitPath
{
    internal const int MaxPath = 4096;
    internal const int Win32MaxPath = 260;

    internal static readonly StringPool PathPool = new();

    [Flags]
    public enum ValidationFlags : uint
    {
        RejectEmptyComponent = 1,
        RejectTraversal = 1 << 1,
        RejectSlash = 1 << 2,
        RejectBackSlash = 1 << 3,
        RejectTrailingDot = 1 << 4,
        RejectTrailingSpace = 1 << 5,
        RejectTrailingColon = 1 << 6,
        RejectDOSPaths = 1 << 7,
        RejectNTChars = 1 << 8,
        RejectLongPaths = 1 << 9,

        // Extended flags
        RejectDotGit = 1 << 10,
        RejectDotGitLiteral = 1 << 11,
        RejectDotGitHFS = 1 << 12,
        RejectDotGitNTFS = 1 << 13,

        RejectAll = uint.MaxValue
    }

    public static ValidationFlags DefaultValidation
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            if (OperatingSystem.IsWindows())
            {
                return ValidationFlags.RejectEmptyComponent
                    | ValidationFlags.RejectTraversal
                    | ValidationFlags.RejectBackSlash
                    | ValidationFlags.RejectTrailingDot
                    | ValidationFlags.RejectTrailingSpace
                    | ValidationFlags.RejectTrailingColon
                    | ValidationFlags.RejectDOSPaths
                    | ValidationFlags.RejectNTChars;
            }
            else
            {
                return ValidationFlags.RejectEmptyComponent
                    | ValidationFlags.RejectTraversal;
            }
        }
    }

    public static ValidationFlags DefaultWorkDirectoryPathValidation => DefaultValidation | ValidationFlags.RejectDotGit;

    public const ValidationFlags DefaultIndexPathValidation = ValidationFlags.RejectTraversal | ValidationFlags.RejectDotGit;

    public enum GitFile
    {
        GitIgnore,
        GitModules,
        GitAttributes
    }

    public enum FileSystem
    {
        Generic,
        NTFS,
        HFS
    }

    internal interface IPathValidator
    {
        bool ValidateCharacter(char c) => true;

        bool ValidateComponent(ReadOnlySpan<char> component) => true;

        bool ValidateLength(ReadOnlySpan<char> path, int unicodeCharacters) => true;
    }

    internal static bool ValidateFileSystemPath<TValidator>(ReadOnlySpan<char> path, ValidationFlags flags, TValidator validator)
        where TValidator: IPathValidator
    {
        if (flags == 0)
            return true;

        if (Path.IsPathRooted(path)) // allow validating fully qualified windows paths
            path = path.Slice(Path.GetPathRoot(path).Length);

        int start = 0;
        for (int i = 0; i < path.Length; ++i)
        {
            var c = path[i];

            if (!ValidateCharacter(c, flags))
                return false;

            if (!validator.ValidateCharacter(c))
                return false;

            if (c != '/')
                continue;

            var component = path[start..i];

            if (!ValidateComponent(component, flags))
                return false;

            if (!validator.ValidateComponent(component))
                return false;

            start = i + 1;
        }

        if ((uint)start < (uint)path.Length)
        {
            if (!ValidateComponent(path.Slice(start), flags))
                return false;

            if (!validator.ValidateComponent(path.Slice(start)))
                return false;
        }

        if (OperatingSystem.IsWindows() && (flags & ValidationFlags.RejectLongPaths) != 0)
        {
            int codeUnits = GetUnicodeCharacterLength(path);

            if (codeUnits > Win32MaxPath || !validator.ValidateLength(path, codeUnits))
                return false;
        }

        return true;

        static bool ValidateCharacter(char c, ValidationFlags flags)
        {
            if (c == '\0') // In C#, the null character is a valid character in strings, but not paths
                return false;

            if ((flags & ValidationFlags.RejectBackSlash) != 0 && c == '\\')
                return false;

            if ((flags & ValidationFlags.RejectSlash) != 0 && c == '/')
                return false;

            if ((flags & ValidationFlags.RejectNTChars) != 0)
            {
                if ((uint)c < 32 || c is '<' or '>' or ':' or '"' or '|' or '?' or '*')
                    return false;
            }

            return true;
        }

        static bool ValidateComponent(ReadOnlySpan<char> component, ValidationFlags flags)
        {
            if (component.Length == 0)
                return (flags & ValidationFlags.RejectEmptyComponent) == 0;

            if ((flags & ValidationFlags.RejectTraversal) != 0 && component is "." or "..")
                return false;

            if ((flags & ValidationFlags.RejectTrailingDot) != 0 && component.EndsWith('.'))
                return false;

            if ((flags & ValidationFlags.RejectTrailingSpace) != 0 && component.EndsWith(' '))
                return false;

            if ((flags & ValidationFlags.RejectTrailingColon) != 0 && component.EndsWith(':'))
                return false;

            if ((flags & ValidationFlags.RejectDOSPaths) != 0)
            {
                if (!ValidateDosPath(component, "CON", false)
                    || !ValidateDosPath(component, "PRN", false)
                    || !ValidateDosPath(component, "AUX", false)
                    || !ValidateDosPath(component, "NUL", false)
                    || !ValidateDosPath(component, "COM", true)
                    || !ValidateDosPath(component, "LPT", true))
                    return false;
            }

            return true;

            static bool ValidateDosPath(ReadOnlySpan<char> component, ReadOnlySpan<char> dospath, bool trailingNum)
            {
                Debug.Assert(dospath.Length == 3);

                int last = trailingNum ? 4 : 3;

                if (component.Length < last || !component[..3].Equals(dospath, StringComparison.OrdinalIgnoreCase))
                    return true;

                if (trailingNum && !char.IsBetween(component[3], '0', '9'))
                    return true;

                return (uint)last < (uint)component.Length && component[last] is not '.' and not ':';
            }
        }
    }

    public static bool PathStringIsValid(GitRepository? repo, ReadOnlySpan<char> path, FileAttributes filemode, ValidationFlags flags)
    {
        // Upgrade the ".git" checks based on platform
        if ((flags & ValidationFlags.RejectDotGit) != 0)
            flags = DotGitFlags(repo, flags);

        // Update the length checks based on platform
        if ((flags & ValidationFlags.RejectLongPaths) != 0)
            flags = LengthFlags(repo, flags);

        return ValidateFileSystemPath(path, flags, new RepoPathValidator(repo, filemode, flags));
    }

    public static void ValidatePathLength(GitRepository? repo, ReadOnlySpan<char> path)
    {
        if (!PathStringIsValid(repo, path, default, ValidationFlags.RejectLongPaths))
        {
            throw new Git2Exception($"Path too long: [{path.Length} characters] \"{path}\"");
        }
    }

    private static readonly (string file, string hash)[] GitFiles = [
        ("gitignore", "gi250a"),
        ("gitmodules", "gi7eba"),
        ("gitattributes", "gi7d29")
    ];

    public static bool PathIsGitFile(ReadOnlySpan<char> path, GitFile gitFile, FileSystem fs)
    {
        if (!Enum.IsDefined(gitFile))
            throw new ArgumentOutOfRangeException(nameof(gitFile), "Invalid gitfile for path validation");

        var (file, hash) = GitFiles[(int)gitFile];

        switch (fs)
        {
            case FileSystem.Generic:
                return !ValidateDotGitNTFSGeneric(path, file, hash)
                    || !ValidateDotGitHFSGeneric(path, file);
            case FileSystem.NTFS:
                return !ValidateDotGitNTFSGeneric(path, file, hash);
            case FileSystem.HFS:
                return !ValidateDotGitHFSGeneric(path, file);
            default:
                throw new ArgumentOutOfRangeException(nameof(fs), "Invalid filesystem for path validation");
        }
    }

    private static bool ValidateDotGitNTFSGeneric(ReadOnlySpan<char> name, ReadOnlySpan<char> dotgitName, ReadOnlySpan<char> shortname_prefix)
    {
        if (name.StartsWith('.') && name.Length >= dotgitName.Length && !name.Slice(1).StartsWith(dotgitName, StringComparison.OrdinalIgnoreCase))
        {
            return !NTFSEndOfFilename(name.Slice(dotgitName.Length));
        }

        if (name.Length >= 8 && dotgitName.Length >= 6
            && name.Slice(0, 6).Equals(dotgitName.Slice(0, 6), StringComparison.OrdinalIgnoreCase)
            && name[6] == '~' && char.IsBetween(name[7], '1', '4'))
        {
            return !NTFSEndOfFilename(name.Slice(8));
        }

        int i = 0;
        bool saw_tilde = false;
        for (; i < 8; ++i)
        {
            if ((uint)i >= (uint)name.Length)
                return true;

            if (saw_tilde)
            {
                if (!char.IsBetween(name[i], '0', '9'))
                    return true;
            }
            else if (name[i] == '~')
            {
                if ((uint)i + 1 >= (uint)name.Length || !char.IsBetween(name[i + 1], '0', '9'))
                    return true;

                saw_tilde = true;
            }
            else if (i >= 6 || name[i] > 127 || char.ToLower(name[i]) != shortname_prefix[i])
            {
                return true;
            }
        }

        return !NTFSEndOfFilename(name.Slice(i));

        static bool NTFSEndOfFilename(ReadOnlySpan<char> path)
        {
            int c = 0;
            for (; ; ++c)
            {
                if ((uint)c >= (uint)path.Length || path[c] == ':')
                    return true;

                if (path[c] is not ' ' and not '.')
                    return false;
            }
        }
    }

    private static bool ValidateDotGitHFSGeneric(ReadOnlySpan<char> name, ReadOnlySpan<char> needle)
    {
        if (NextHFSCharacter(ref name) != (Rune)'.')
        {
            return true;
        }

        Debug.Assert(!needle.ContainsAnyExceptInRange('\0', '\x007f'));

        for (int i = 0; i < needle.Length; ++i)
        {
            var rune = NextHFSCharacter(ref name);

            if (rune != (Rune)needle[i])
                return true;
        }

        if (NextHFSCharacter(ref name) == null)
        {
            return true;
        }

        return false;

        static Rune? NextHFSCharacter(ref ReadOnlySpan<char> @in)
        {
            var span = @in;
            while (span.Length > 0)
            {
                var result = Rune.DecodeFromUtf16(span, out var rune, out int consumed);

                if (result != System.Buffers.OperationStatus.Done)
                {
                    return null;
                }

                span = span.Slice(consumed);

                // These characters are ignored completely
                switch (rune.Value)
                {
                    case 0x200c: // Zero Width Non-Joiner
                    case 0x200d: // Zero Width Joiner
                    case 0x200e: // Left to Right Mark
                    case 0x200f: // Right to Left Mark
                    case 0x202a: // Left to Right Embedding
                    case 0x202b: // Right to Left Embedding
                    case 0x202c: // Pop Directional Formatting
                    case 0x202d: // Left to Right Override
                    case 0x202e: // Right to Left Override
                    case 0x206a: // Inhibit Symmetric Swapping
                    case 0x206b: // Activate Symmetric Swapping
                    case 0x206c: // Inhibit Arabic Form Shaping
                    case 0x206d: // Activate Arabic Form Shaping
                    case 0x206e: // National Digit Shapes
                    case 0x206f: // Nominal Digit Shapes
                    case 0xfeff: // Zero Width No-Break Space
                        continue;
                }

                @in = span;
                return Rune.ToLowerInvariant(rune);
            }

            return null;
        }
    }

    private static bool ValidateDotGitNTFS(GitRepository? repo, ReadOnlySpan<char> path)
    {
        int start = 0;

        foreach (var r in repo?.GetReservedNames(true) ?? GitRepository.ReservedNamesWin32)
        {
            if (path.StartsWith(r, StringComparison.OrdinalIgnoreCase))
            {
                start = r.Length;
                break;
            }
        }

        if (start == 0)
            return true;

        if ((uint)start < (uint)path.Length && path[start] is not ':' and not '\\')
        {
            for (int i = start; i < path.Length; i++)
            {
                if (path[i] is not ' ' and not '.')
                    return true;
            }
        }

        return false;
    }

    private static ValidationFlags DotGitFlags(GitRepository? repo, ValidationFlags flags)
    {
        flags |= ValidationFlags.RejectDotGitLiteral;

        bool protectHFS = OperatingSystem.IsMacOS() | OperatingSystem.IsMacCatalyst() | OperatingSystem.IsIOS(); // Is apple platform
        bool protectNTFS = true;

        if (repo is not null)
        {
            if (!protectHFS && repo.TryConfigmapLookup(GitConfigMapItem.ProtectHFS, out bool tmp))
                protectHFS = tmp;

            if (repo.TryConfigmapLookup(GitConfigMapItem.ProtectNTFS, out tmp))
                protectNTFS = tmp;
        }

        if (protectHFS)
        {
            flags |= ValidationFlags.RejectDotGitHFS;
        }

        if (protectNTFS)
        {
            flags |= ValidationFlags.RejectDotGitNTFS;
        }

        return flags;
    }

    private static ValidationFlags LengthFlags(GitRepository? repo, ValidationFlags flags)
    {
        if (OperatingSystem.IsWindows())
        {
            bool allow = false;

            if (repo is not null && !repo.TryConfigmapLookup(GitConfigMapItem.LongPaths, out allow))
                allow = false;

            if (allow)
                flags &= ~ValidationFlags.RejectLongPaths;
        }
        else
        {
            flags &= ~ValidationFlags.RejectLongPaths;
        }

        return flags;
    }

    internal static bool IsDirectorySeparator(char c)
    {
        return c == '/' || (OperatingSystem.IsWindows() && c == '\\');
    }

    internal static bool EndsWithDirectorySeparator(ReadOnlySpan<char> path)
    {
        return path.EndsWith('/') || (OperatingSystem.IsWindows() && path.EndsWith('\\'));
    }

    internal static ReadOnlySpan<char> TrimEndingDirectorySeparator(ReadOnlySpan<char> path)
    {
        return EndsWithDirectorySeparator(path) ? path[..^1] : path;
    }

    public static string PosixJoin(string? path1, string? path2)
    {
        if (string.IsNullOrEmpty(path1))
            return path2 ?? "";

        if (string.IsNullOrEmpty(path2))
            return path1;

        return PosixJoin(path1.AsSpan(), path2.AsSpan());
    }

    public static string PosixJoin(ReadOnlySpan<char> path1, ReadOnlySpan<char> path2)
    {
        if (path1.IsEmpty)
            return path2.IsEmpty ? "" : path2.ToString();

        if (path2.IsEmpty)
            return path1.ToString();

        if (OperatingSystem.IsWindows())
        {
            if (IsDirectorySeparator(path1[^1]))
                path1 = path1[..^1];

            if (IsDirectorySeparator(path2[0]))
                path2 = path2[1..];

            return string.Concat(path1, "/", path2);
        }
        else
        {
            int x = Unsafe.BitCast<bool, byte>(path1.EndsWith('/'));
            Debug.Assert(x <= 1); // Assert that the bool was 0 or 1

            int y = Unsafe.BitCast<bool, byte>(path2.StartsWith('/'));
            Debug.Assert(y <= 1);

            x += y;

            switch (x)
            {
                case 0:
                    return string.Concat(path1, "/", path2);
                case 1:
                    return string.Concat(path1, path2);
                case 2:
                    path2 = path2[1..];
                    goto case 1;
                default:
                    throw new UnreachableException();
            }
        }
    }

    public static void WalkUp(string path, string? ceiling, Action<ReadOnlySpan<char>> callback)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(callback);

        int stop = 0;
        if (ceiling != null)
        {
            stop = path.StartsWith(ceiling, OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal)
                ? ceiling.Length
                : path.Length;
        }

        if (path.Length == 0)
        {
            callback("");
            return;
        }

        var iter = path.AsSpan();
        var scan = iter.Length;

        var dsc = Path.DirectorySeparatorChar;
        var adsc = Path.AltDirectorySeparatorChar;

        while (scan >= stop)
        {
            callback(iter);

            if (iter.EndsWith(dsc) || (dsc != adsc && iter.EndsWith(adsc)))
                iter = iter[..^1];

            if (dsc == adsc)
            {
                scan = iter.LastIndexOf(dsc);
            }
            else
            {
                scan = iter.LastIndexOfAny(dsc, adsc);
            }

            if (scan >= 0)
            {
                iter = iter.Slice(0, scan + 1);
            }
        }

        if (stop == 0 && !Path.IsPathRooted(iter))
        {
            callback("");
        }
    }

    public static WalkUpEnumerator WalkUp(string path, string? ceiling)
    {
        ArgumentNullException.ThrowIfNull(path);

        return new WalkUpEnumerator(path, ceiling);
    }

    public ref struct WalkUpEnumerator
    {
        private ReadOnlySpan<char> _span;
        private readonly int _stop;
        private int _state;

        public WalkUpEnumerator(ReadOnlySpan<char> path, ReadOnlySpan<char> ceiling)
        {
            _span = path;
            _state = 0;

            if (!ceiling.IsEmpty)
            {
                if (path.StartsWith(ceiling, OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
                {
                    _stop = ceiling.Length;
                }
                else
                {
                    _stop = path.Length;
                }
            }
            else
            {
                _stop = 0;
            }
        }

        public bool MoveNext()
        {
            if (_state < 0)
                return false;

            if (_state == 0)
            {
                _state = _span.IsEmpty ? -1 : 1; // Return original path once
                return true;
            }

            // This assumes that a path only uses one directory separator to separate directories.
            var span = Path.TrimEndingDirectorySeparator(_span);

            int idx;
            idx = Path.DirectorySeparatorChar == Path.AltDirectorySeparatorChar
                ? span.LastIndexOf(Path.DirectorySeparatorChar)
                : span.LastIndexOfAny(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            if (idx >= 0)
            {
                if (idx + 1 < _stop)
                {
                    _state = -1;
                    return false;
                }

                _span = span = span.Slice(0, idx + 1);

                if (Path.GetPathRoot(span) == span)
                {
                    _state = -1;
                }

                return true;
            }
            else
            {
                _state = -1;

                return _stop == 0 && !Path.IsPathRooted(span);
            }
        }

        public readonly ReadOnlySpan<char> Current => _span;

        public WalkUpEnumerator GetEnumerator()
        {
            return this;
        }
    }

    internal static int ComparePaths(ReadOnlySpan<char> path1, bool isDir1, ReadOnlySpan<char> path2, bool isDir2)
    {
        Debug.Assert(!EndsWithDirectorySeparator(path1));
        Debug.Assert(!EndsWithDirectorySeparator(path2));

        int minLen = Math.Min(path1.Length, path2.Length);

        if (minLen == 0)
        {
            return path1.Length.CompareTo(path2.Length);
        }

        int cmp;
        if (OperatingSystem.IsWindows())
        {
            // 'char' isn't a valid vector element, reinterpret as 'ushort'
            ref ushort p1 = ref Unsafe.As<char, ushort>(ref MemoryMarshal.GetReference(path1));
            ref ushort p2 = ref Unsafe.As<char, ushort>(ref MemoryMarshal.GetReference(path2));

            cmp = ComparePathsWin32(ref p1, ref p2, minLen);
        }
        else
        {
            var p1 = path1.Slice(0, minLen);
            var p2 = path2.Slice(0, minLen);

            cmp = p1.SequenceCompareTo(p2);
        }

        if (cmp != 0)
            return cmp;

        char c1;
        if ((uint)minLen < (uint)path1.Length)
        {
            c1 = path1[minLen];

            if (OperatingSystem.IsWindows() && c1 == '\\')
                c1 = '/';
        }
        else
        {
            c1 = isDir1 ? '/' : '\0';
        }

        char c2;
        if ((uint)minLen < (uint)path2.Length)
        {
            c2 = path2[minLen];

            if (OperatingSystem.IsWindows() && c2 == '\\')
                c2 = '/';
        }
        else
        {
            c2 = isDir2 ? '/' : '\0';
        }

        return c1.CompareTo(c2);

        static int ComparePathsWin32(ref ushort p1, ref ushort p2, int length)
        {
            Debug.Assert(length > 0);
            ushort comparand = '\\', replacement = '/';

            if (Vector512.IsHardwareAccelerated && (uint)length >= (uint)Vector512<ushort>.Count)
            {
                var vComparand = Vector512.Create(comparand);
                var vReplacement = Vector512.Create(replacement);
                Vector512<ushort> p1Val, p2Val;

                ref ushort minusOneVec = ref Unsafe.Add(ref p1, length - Vector512<ushort>.Count);
                ref ushort minusOneVec2 = ref Unsafe.Add(ref p2, length - Vector512<ushort>.Count);

                while (Unsafe.IsAddressLessThan(ref p1, ref minusOneVec))
                {
                    p1Val = Vector512.LoadUnsafe(ref p1);
                    p2Val = Vector512.LoadUnsafe(ref p2);

                    p1Val = Vector512.ConditionalSelect(Vector512.Equals(p1Val, vComparand), vReplacement, p1Val);
                    p2Val = Vector512.ConditionalSelect(Vector512.Equals(p2Val, vComparand), vReplacement, p2Val);

                    if (p1Val != p2Val)
                    {
                        int index = Vector512.IndexOfWhereAllBitsSet(~Vector512.Equals(p1Val, p2Val));

                        // guarenteed to not be equal, so don't check for it
                        return p1Val[index] < p2Val[index] ? -1 : 1;
                    }

                    p1 = ref Unsafe.Add(ref p1, Vector512<ushort>.Count);
                    p2 = ref Unsafe.Add(ref p2, Vector512<ushort>.Count);
                }

                p1Val = Vector512.LoadUnsafe(ref minusOneVec);
                p2Val = Vector512.LoadUnsafe(ref minusOneVec2);

                p1Val = Vector512.ConditionalSelect(Vector512.Equals(p1Val, vComparand), vReplacement, p1Val);
                p2Val = Vector512.ConditionalSelect(Vector512.Equals(p2Val, vComparand), vReplacement, p2Val);

                if (p1Val != p2Val)
                {
                    int index = Vector512.IndexOfWhereAllBitsSet(~Vector512.Equals(p1Val, p2Val));

                    // guarenteed to not be equal, so don't check for it
                    return p1Val[index] < p2Val[index] ? -1 : 1;
                }
            }
            else if (Vector256.IsHardwareAccelerated && (uint)length >= (uint)Vector256<ushort>.Count)
            {
                var vComparand = Vector256.Create(comparand);
                var vReplacement = Vector256.Create(replacement);
                Vector256<ushort> p1Val, p2Val;

                ref ushort minusOneVec = ref Unsafe.Add(ref p1, length - Vector256<ushort>.Count);
                ref ushort minusOneVec2 = ref Unsafe.Add(ref p2, length - Vector256<ushort>.Count);

                while (Unsafe.IsAddressLessThan(ref p1, ref minusOneVec))
                {
                    p1Val = Vector256.LoadUnsafe(ref p1);
                    p2Val = Vector256.LoadUnsafe(ref p2);

                    p1Val = Vector256.ConditionalSelect(Vector256.Equals(p1Val, vComparand), vReplacement, p1Val);
                    p2Val = Vector256.ConditionalSelect(Vector256.Equals(p2Val, vComparand), vReplacement, p2Val);

                    if (p1Val != p2Val)
                    {
                        int index = Vector256.IndexOfWhereAllBitsSet(~Vector256.Equals(p1Val, p2Val));

                        // guarenteed to not be equal, so don't check for it
                        return p1Val[index] < p2Val[index] ? -1 : 1;
                    }

                    p1 = ref Unsafe.Add(ref p1, Vector512<ushort>.Count);
                    p2 = ref Unsafe.Add(ref p2, Vector512<ushort>.Count);
                }

                p1Val = Vector256.LoadUnsafe(ref minusOneVec);
                p2Val = Vector256.LoadUnsafe(ref minusOneVec2);

                p1Val = Vector256.ConditionalSelect(Vector256.Equals(p1Val, vComparand), vReplacement, p1Val);
                p2Val = Vector256.ConditionalSelect(Vector256.Equals(p2Val, vComparand), vReplacement, p2Val);

                if (p1Val != p2Val)
                {
                    int index = Vector256.IndexOfWhereAllBitsSet(~Vector256.Equals(p1Val, p2Val));

                    // guarenteed to not be equal, so don't check for it
                    return p1Val[index] < p2Val[index] ? -1 : 1;
                }
            }
            else if (Vector128.IsHardwareAccelerated && (uint)length >= (uint)Vector128<ushort>.Count)
            {
                var vComparand = Vector128.Create(comparand);
                var vReplacement = Vector128.Create(replacement);
                Vector128<ushort> p1Val, p2Val;

                ref ushort minusOneVec = ref Unsafe.Add(ref p1, length - Vector128<ushort>.Count);
                ref ushort minusOneVec2 = ref Unsafe.Add(ref p2, length - Vector128<ushort>.Count);

                while (Unsafe.IsAddressLessThan(ref p1, ref minusOneVec))
                {
                    p1Val = Vector128.LoadUnsafe(ref p1);
                    p2Val = Vector128.LoadUnsafe(ref p2);

                    p1Val = Vector128.ConditionalSelect(Vector128.Equals(p1Val, vComparand), vReplacement, p1Val);
                    p2Val = Vector128.ConditionalSelect(Vector128.Equals(p2Val, vComparand), vReplacement, p2Val);

                    if (p1Val != p2Val)
                    {
                        int index = Vector128.IndexOfWhereAllBitsSet(~Vector128.Equals(p1Val, p2Val));

                        // guarenteed to not be equal, so don't check for it
                        return p1Val[index] < p2Val[index] ? -1 : 1;
                    }

                    p1 = ref Unsafe.Add(ref p1, Vector512<ushort>.Count);
                    p2 = ref Unsafe.Add(ref p2, Vector512<ushort>.Count);
                }

                p1Val = Vector128.LoadUnsafe(ref minusOneVec);
                p2Val = Vector128.LoadUnsafe(ref minusOneVec2);

                p1Val = Vector128.ConditionalSelect(Vector128.Equals(p1Val, vComparand), vReplacement, p1Val);
                p2Val = Vector128.ConditionalSelect(Vector128.Equals(p2Val, vComparand), vReplacement, p2Val);

                if (p1Val != p2Val)
                {
                    int index = Vector128.IndexOfWhereAllBitsSet(~Vector128.Equals(p1Val, p2Val));

                    // guarenteed to not be equal, so don't check for it
                    return p1Val[index] < p2Val[index] ? -1 : 1;
                }
            }
            else
            {
                for (int i = 0; i < length; ++i)
                {
                    ushort p1Val = Unsafe.Add(ref p1, i);
                    ushort p2Val = Unsafe.Add(ref p2, i);

                    if (p1Val == comparand)
                        p1Val = replacement;

                    if (p2Val == comparand)
                        p2Val = replacement;

                    if (p1Val != p2Val)
                        return p1Val < p2Val ? -1 : 1;
                }
            }

            return 0;
        }
    }

    public static bool ArePathsEqual(ReadOnlySpan<char> path1, ReadOnlySpan<char> path2)
    {
        path1 = TrimEndingDirectorySeparator(path1);
        path2 = TrimEndingDirectorySeparator(path2);

        if (!OperatingSystem.IsWindows())
        {
            // '\' characters are not path separators on non-windows platforms, and thus don't need to be considered.
            return path1.SequenceEqual(path2);
        }
        else
        {
            if (path1.Length != path2.Length)
                return false;

            if (path1.Length == 0)
                return true;

            ref ushort p1 = ref Unsafe.As<char, ushort>(ref MemoryMarshal.GetReference(path1));
            ref ushort p2 = ref Unsafe.As<char, ushort>(ref MemoryMarshal.GetReference(path2));

            if (Unsafe.AreSame(ref p1, ref p2))
                return true;

            // '\' and '/' need to be considered equal
            return _ImplWin32(ref p1, ref p2, path1.Length);
        }

        static bool _ImplWin32(ref ushort p1, ref ushort p2, int length)
        {
            Debug.Assert(length > 0);

            ushort comparand = '\\', replacement = '/';

            if (Vector512.IsHardwareAccelerated && (uint)length >= (uint)Vector512<ushort>.Count)
            {
                var vComparand = Vector512.Create(comparand);
                var vReplacement = Vector512.Create(replacement);
                Vector512<ushort> p1Val, p2Val;

                ref ushort minusOneVec = ref Unsafe.Add(ref p1, length - Vector512<ushort>.Count);
                ref ushort minusOneVec2 = ref Unsafe.Add(ref p2, length - Vector512<ushort>.Count);

                while (Unsafe.IsAddressLessThan(ref p1, ref minusOneVec))
                {
                    p1Val = Vector512.LoadUnsafe(ref p1);
                    p2Val = Vector512.LoadUnsafe(ref p2);

                    p1Val = Vector512.ConditionalSelect(Vector512.Equals(p1Val, vComparand), vReplacement, p1Val);
                    p2Val = Vector512.ConditionalSelect(Vector512.Equals(p2Val, vComparand), vReplacement, p2Val);

                    if (p1Val != p2Val)
                    {
                        return false;
                    }

                    p1 = ref Unsafe.Add(ref p1, Vector512<ushort>.Count);
                    p2 = ref Unsafe.Add(ref p2, Vector512<ushort>.Count);
                }

                p1Val = Vector512.LoadUnsafe(ref minusOneVec);
                p2Val = Vector512.LoadUnsafe(ref minusOneVec2);

                p1Val = Vector512.ConditionalSelect(Vector512.Equals(p1Val, vComparand), vReplacement, p1Val);
                p2Val = Vector512.ConditionalSelect(Vector512.Equals(p2Val, vComparand), vReplacement, p2Val);

                if (p1Val != p2Val)
                {
                    return false;
                }
            }
            else if (Vector256.IsHardwareAccelerated && (uint)length >= (uint)Vector256<ushort>.Count)
            {
                var vComparand = Vector256.Create(comparand);
                var vReplacement = Vector256.Create(replacement);
                Vector256<ushort> p1Val, p2Val;

                ref ushort minusOneVec = ref Unsafe.Add(ref p1, length - Vector256<ushort>.Count);
                ref ushort minusOneVec2 = ref Unsafe.Add(ref p2, length - Vector256<ushort>.Count);

                while (Unsafe.IsAddressLessThan(ref p1, ref minusOneVec))
                {
                    p1Val = Vector256.LoadUnsafe(ref p1);
                    p2Val = Vector256.LoadUnsafe(ref p2);

                    p1Val = Vector256.ConditionalSelect(Vector256.Equals(p1Val, vComparand), vReplacement, p1Val);
                    p2Val = Vector256.ConditionalSelect(Vector256.Equals(p2Val, vComparand), vReplacement, p2Val);

                    if (p1Val != p2Val)
                    {
                        return false;
                    }

                    p1 = ref Unsafe.Add(ref p1, Vector256<ushort>.Count);
                    p2 = ref Unsafe.Add(ref p2, Vector256<ushort>.Count);
                }

                p1Val = Vector256.LoadUnsafe(ref minusOneVec);
                p2Val = Vector256.LoadUnsafe(ref minusOneVec2);

                p1Val = Vector256.ConditionalSelect(Vector256.Equals(p1Val, vComparand), vReplacement, p1Val);
                p2Val = Vector256.ConditionalSelect(Vector256.Equals(p2Val, vComparand), vReplacement, p2Val);

                if (p1Val != p2Val)
                {
                    return false;
                }
            }
            else if (Vector128.IsHardwareAccelerated && (uint)length >= (uint)Vector128<ushort>.Count)
            {
                var vComparand = Vector128.Create(comparand);
                var vReplacement = Vector128.Create(replacement);
                Vector128<ushort> p1Val, p2Val;

                ref ushort minusOneVec = ref Unsafe.Add(ref p1, length - Vector128<ushort>.Count);
                ref ushort minusOneVec2 = ref Unsafe.Add(ref p2, length - Vector128<ushort>.Count);

                while (Unsafe.IsAddressLessThan(ref p1, ref minusOneVec))
                {
                    p1Val = Vector128.LoadUnsafe(ref p1);
                    p2Val = Vector128.LoadUnsafe(ref p2);

                    p1Val = Vector128.ConditionalSelect(Vector128.Equals(p1Val, vComparand), vReplacement, p1Val);
                    p2Val = Vector128.ConditionalSelect(Vector128.Equals(p2Val, vComparand), vReplacement, p2Val);

                    if (p1Val != p2Val)
                    {
                        return false;
                    }

                    p1 = ref Unsafe.Add(ref p1, Vector128<ushort>.Count);
                    p2 = ref Unsafe.Add(ref p2, Vector128<ushort>.Count);
                }

                p1Val = Vector128.LoadUnsafe(ref minusOneVec);
                p2Val = Vector128.LoadUnsafe(ref minusOneVec2);

                p1Val = Vector128.ConditionalSelect(Vector128.Equals(p1Val, vComparand), vReplacement, p1Val);
                p2Val = Vector128.ConditionalSelect(Vector128.Equals(p2Val, vComparand), vReplacement, p2Val);

                if (p1Val != p2Val)
                {
                    return false;
                }
            }
            else
            {
                for (int i = 0; i < length; ++i)
                {
                    ushort p1Val = Unsafe.Add(ref p1, i);
                    ushort p2Val = Unsafe.Add(ref p2, i);

                    if (p1Val == comparand)
                        p1Val = replacement;

                    if (p2Val == comparand)
                        p2Val = replacement;

                    if (p1Val != p2Val)
                        return false;
                }
            }

            return true;
        }
    }

    /// <summary>
    /// Maintains the original behavior of 
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public static unsafe string? RealPath(string path)
    {
        if (OperatingSystem.IsWindows())
        {
            var fullpath = Path.GetFullPath(path);

            int maxLen = fullpath.StartsWith("//?/") || fullpath.StartsWith("\\\\?\\") ? 32_767 : Win32MaxPath;

            char[] buffer = ArrayPool<char>.Shared.Rent(maxLen);

            try
            {
                int outputLen;
                fixed (char* p = fullpath /*.NET strings are null terminated by default*/, b = buffer)
                {
                    outputLen = GetLongPathName(p, b, maxLen);
                }

                if (outputLen == 0)
                {
                    // An error occured
                    return null;
                }

                Debug.Assert(outputLen <= maxLen);

                Span<char> outputPath = buffer.AsSpan(0, outputLen);
                outputPath.Replace('\\', '/');

                return outputPath.SequenceEqual(path) ? path : outputPath.ToString();
            }
            finally
            {
                ArrayPool<char>.Shared.Return(buffer);
            }

            [DllImport("kernel32.dll", EntryPoint = "GetLongPathNameW", SetLastError = true)]
            static extern int GetLongPathName(char* shortPath, char* longPathBuffer, int bufferLen);
        }
        else
        {
            int maxLen = checked(Encoding.UTF8.GetMaxByteCount(path.Length) + 1);

            byte[] bytePath = ArrayPool<byte>.Shared.Rent(maxLen);

            try
            {
                int actualLen = Encoding.UTF8.GetBytes(path, bytePath);

                bytePath[actualLen] = 0; // null-terminate

                byte* buffer = stackalloc byte[MaxPath + 1]; // Auto-initialized to 0
                byte* ret;

                fixed (byte* bPath = bytePath)
                {
                    ret = _realPath(bPath, buffer);
                }

                if (ret == null)
                    return null;

                var span = new ReadOnlySpan<byte>(buffer, MaxPath);

                int nullTerminator = span.IndexOf((byte)0);

                if (nullTerminator >= 0)
                    span = span[..nullTerminator];

                string? retVal;
                if (span.SequenceEqual(bytePath.AsSpan(0, actualLen)))
                {
                    retVal = path; // potentially avoid an unnecessary allocation
                }
                else
                {
                    retVal = Encoding.UTF8.GetString(span);
                }

                if (OperatingSystem.IsFreeBSD() && !Path.Exists(retVal))
                {
                    retVal = null;
                }

                return retVal;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(bytePath);
            }

            [DllImport("libc", EntryPoint = "realpath")]
            static extern byte* _realPath(byte* input, byte* outputBuffer);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int GetUnicodeCharacterLength(ReadOnlySpan<char> span)
    {
        const char surrogate_range_start = '\ud800', surrogate_range_end = '\udfff';

        int charLen = span.IndexOfAnyInRange(surrogate_range_start, surrogate_range_end);
        if (charLen < 0)
        {
            // no multi-unit characters in path, use span length
            return span.Length;
        }
        else
        {
            var enumerator = span.Slice(charLen).EnumerateRunes();
            while (enumerator.MoveNext())
            {
                charLen += 1;
            }

            return charLen;
        }
    }

    public static bool ValidateStringLengthWithSuffix(ReadOnlySpan<char> path, int suffixLen)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(suffixLen);

        if (OperatingSystem.IsWindows())
        {
            int charLen = GetUnicodeCharacterLength(path);

            if (checked(charLen + suffixLen) > 260)
            {
                return false;
            }
        }

        return true;
    }

#if DEBUG
    public static PathOwnerType MockOwner { get; set; }
#endif

    public static bool OwnerIs(string path, PathOwnerType ownerType)
    {
#if DEBUG
        var mock = MockOwner;
        if (mock != PathOwnerType.None)
        {
            return (mock & ownerType) != 0;
        }
#endif

        if (OperatingSystem.IsWindows())
        {
            return _Win32(path, ownerType);
        }
        else
        {
            return _Posix(path, ownerType);
        }

        static unsafe bool _Win32(string path, PathOwnerType ownerType)
        {
            void* owner_sid = null, user_sid = null;

            try
            {
                owner_sid = FileOwnerSID(path);

                if ((ownerType & PathOwnerType.CurrentUser) != 0)
                {
                    user_sid = CurrentUserSID();

                    if (Windows.EqualSid(owner_sid, user_sid))
                    {
                        return true;
                    }
                }

                bool admin_owned = Windows.IsWellKnownSid(owner_sid, WELL_KNOWN_SID_TYPE.WinBuiltinAdministratorsSid)
                    || Windows.IsWellKnownSid(owner_sid, WELL_KNOWN_SID_TYPE.WinLocalSystemSid);

                if (admin_owned && (ownerType & PathOwnerType.Administrator) != 0)
                {
                    return true;
                }

                BOOL is_admin = default;
                if (admin_owned && (ownerType & PathOwnerType.UserIsAdministrator) != 0 && Windows.CheckTokenMembership(default, owner_sid, &is_admin) && is_admin)
                {
                    return true;
                }

                return false;
            }
            finally
            {
                NativeMemory.Free(owner_sid);
                NativeMemory.Free(user_sid);
            }

            static unsafe void* FileOwnerSID(string path)
            {
                if (!OperatingSystem.IsWindowsVersionAtLeast(6, 1))
                    throw new Git2OSException("Only Windows versions higher than NT 6.1 are supported!");

                void* owner_sid = null, descriptor = null;
                uint ret;

                try
                {
                    fixed (char* p = path)
                    {
                        ret = Windows.GetNamedSecurityInfoW(p, SE_OBJECT_TYPE.SE_FILE_OBJECT, Windows.OWNER_SECURITY_INFORMATION | Windows.DACL_SECURITY_INFORMATION, &owner_sid, null, null, null, &descriptor);
                    }

                    if (ret == ERROR.ERROR_FILE_NOT_FOUND || ret == ERROR.ERROR_PATH_NOT_FOUND)
                    {
                        throw new FileNotFoundException(null, path);
                    }
                    else if (ret != ERROR.ERROR_SUCCESS)
                    {
                        throw new Git2OSException("Failed to get security information!");
                    }
                    else if (!Windows.IsValidSid(owner_sid))
                    {
                        throw new Git2OSException("File owner is not valid!");
                    }

                    return sid_dup(owner_sid);
                }
                finally
                {
                    if (descriptor != null)
                        Windows.LocalFree((HLOCAL)descriptor);
                }
            }

            static void* CurrentUserSID()
            {
                Debug.Assert(OperatingSystem.IsWindowsVersionAtLeast(6, 1));

                using var currentProcess = Process.GetCurrentProcess();

                uint len = 0;
                if (Windows.GetTokenInformation((HANDLE)currentProcess.Handle, TOKEN_INFORMATION_CLASS.TokenUser, null, 0, &len)
                    || Windows.GetLastError() != ERROR.ERROR_INSUFFICIENT_BUFFER)
                {
                    throw new Git2OSException("Could not lookup token metadata!");
                }

                TOKEN_USER* info = (TOKEN_USER*)NativeMemory.AllocZeroed(len);
                if (info == null)
                    throw new OutOfMemoryException();

                try
                {
                    if (!Windows.GetTokenInformation((HANDLE)currentProcess.Handle, TOKEN_INFORMATION_CLASS.TokenUser, info, len, &len))
                    {
                        throw new Git2OSException("Could not lookup current user!");
                    }

                    return sid_dup(info->User.Sid);
                }
                finally
                {
                    NativeMemory.Free(info);
                }
            }

            static void* sid_dup(void* sid)
            {
                Debug.Assert(OperatingSystem.IsWindowsVersionAtLeast(6, 1));

                uint len = Windows.GetLengthSid(sid);

                void* new_sid = NativeMemory.AllocZeroed(len);

                if (new_sid == null)
                    throw new OutOfMemoryException();

                if (!Windows.CopySid(len, new_sid, sid))
                {
                    NativeMemory.Free(new_sid);

                    throw new Git2OSException("Could not duplicate SID!");
                }

                return new_sid;
            }
        }

        static bool _Posix(string path, PathOwnerType ownerType)
        {
            uint euid = Syscall.geteuid();
            if (Syscall.lstat(path, out var stat) != 0)
            {
                throw Syscall.GetLastError() == Errno.ENOENT ? new FileNotFoundException(null, path) : new Git2OSException($"Could not stat '{path}'!");
            }

            if ((ownerType & PathOwnerType.CurrentUser) != 0 && stat.st_uid == euid)
            {
                return true;
            }

            if ((ownerType & PathOwnerType.Administrator) != 0 && stat.st_uid == 0)
            {
                return true;
            }

            if ((ownerType & PathOwnerType.RunningSudo) != 0 && euid == 0)
            {
                if (uint.TryParse(Environment.GetEnvironmentVariable("SUDO_UID"), out uint sudo_uid) && sudo_uid == stat.st_uid)
                {
                    return true;
                }
            }

            return false;
        }

    }

    public static ReadOnlySpan<char> TrimNamespace(ReadOnlySpan<char> path)
    {
        if (OperatingSystem.IsWindows())
        {
            if (path.StartsWith("\\??\\") || path.StartsWith("\\\\?\\"))
            {
                path = path.Slice(4);

                if (path.StartsWith("UNC\\"))
                    path = path.Slice(4);
            }
        }

        return path;
    }

    private record struct RepoPathValidator(GitRepository? Repo, FileAttributes FileMode, ValidationFlags Flags) : IPathValidator
    {
        public readonly bool ValidateComponent(ReadOnlySpan<char> component)
        {
            if ((Flags & ValidationFlags.RejectDotGitHFS) != 0)
            {
                if (!ValidateDotGitHFSGeneric(component, "git"))
                    return false;

                if ((FileMode & FileAttributes.ReparsePoint) != 0 // S_ISLNK replacement
                    && PathIsGitFile(component, GitFile.GitModules, FileSystem.HFS))
                {
                    return false;
                }
            }

            if ((Flags & ValidationFlags.RejectDotGitNTFS) != 0)
            {
                if (!ValidateDotGitNTFS(Repo, component))
                    return false;

                if ((FileMode & FileAttributes.ReparsePoint) != 0 // S_ISLNK replacement
                    && PathIsGitFile(component, GitFile.GitModules, FileSystem.NTFS))
                {
                    return false;
                }
            }

            if ((Flags & (ValidationFlags.RejectDotGitHFS | ValidationFlags.RejectDotGitNTFS)) == 0
                && (Flags & ValidationFlags.RejectDotGitLiteral) != 0)
            {
                if (component.StartsWith(".git", StringComparison.OrdinalIgnoreCase))
                {
                    if (component.Length == 4)
                        return false;

                    if ((FileMode & FileAttributes.ReparsePoint) != 0
                        && component.Equals(".gitmodules", StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }

    public enum PathOwnerType
    {
        None = 0,
        /// <summary>
        /// The file must be owned by the current user.
        /// </summary>
        CurrentUser = 1 << 0,
        /// <summary>
        /// The file must be owned by the system account.
        /// </summary>
        Administrator = 1 << 1,
        /// <summary>
        /// The file may be owned by a system account if the current
        /// user is in an administrator group. Windows only; this is
        /// a no-op on non-Windows systems.
        /// </summary>
        UserIsAdministrator = 1 << 2,
        /// <summary>
        /// The file is owned by the current user, who is running `sudo`.
        /// </summary>
        RunningSudo = 1 << 3,
        /// <summary>
        /// The file may be owned by another user.
        /// </summary>
        Other = 1 << 4,
    }

    public static IEqualityComparer<string> PathEqualityComparer { get; } = OperatingSystem.IsWindows() ? new Win32PathComparer() : StringComparer.Ordinal;
    public static IComparer<string> PathComparer { get; } = OperatingSystem.IsWindows() ? (IComparer<string>)PathEqualityComparer : Comparer<string>.Default;

    internal sealed class Win32PathComparer : IEqualityComparer<string>, IComparer<string>, IAlternateEqualityComparer<ReadOnlySpan<char>, string>
    {
        public bool Equals(string? x, string? y)
        {
            if (ReferenceEquals(x, y))
                return true;

            if (x is null || y is null)
                return false;

            if (x.Length != y.Length)
                return false;

            // Unknown if this is necessary
            if (x.Length == 0)
                return true;

            return PathsAreEqual(
                ref Unsafe.As<char, ushort>(ref x.DangerousGetReference()),
                ref Unsafe.As<char, ushort>(ref y.DangerousGetReference()),
                x.Length);
        }

        public bool Equals(ReadOnlySpan<char> alternate, string other)
        {
            if (alternate.Length != other.Length)
                return false;

            if (alternate.IsEmpty) // Length == 0
                return true;

            return PathsAreEqual(
                ref Unsafe.As<char, ushort>(ref MemoryMarshal.GetReference(alternate)),
                ref Unsafe.As<char, ushort>(ref other.DangerousGetReference()),
                alternate.Length);
        }

        public int GetHashCode([DisallowNull] string obj)
        {
            return GetHashCode(obj.AsSpan());
        }

        public int GetHashCode(ReadOnlySpan<char> alternate)
        {
            if (alternate.IsEmpty)
                return 0;

            char[] buffer = ArrayPool<char>.Shared.Rent(alternate.Length);

            try
            {
                alternate.Replace(buffer, '\\', '/');

                return CommunityToolkit.HighPerformance.Helpers.HashCode<char>.Combine(buffer.AsSpan(0, alternate.Length));
            }
            finally
            {
                ArrayPool<char>.Shared.Return(buffer);
            }
        }

        public int Compare(string? x, string? y)
        {
            throw new NotImplementedException();
        }

        public string Create(ReadOnlySpan<char> alternate)
        {
            return string.Create(alternate.Length, alternate, (output, input) =>
            {
                input.Replace(output, '\\', '/'); // normalize to posix separators
            });
        }

        private static bool PathsAreEqual(ref ushort p1, ref ushort p2, int length)
        {
            Debug.Assert(length > 0);

            ushort comparand = '\\', replacement = '/';

            if (Vector512.IsHardwareAccelerated && (uint)length >= (uint)Vector512<ushort>.Count)
            {
                var vComparand = Vector512.Create(comparand);
                var vReplacement = Vector512.Create(replacement);
                Vector512<ushort> p1Val, p2Val;

                ref ushort minusOneVec = ref Unsafe.Add(ref p1, length - Vector512<ushort>.Count);
                ref ushort minusOneVec2 = ref Unsafe.Add(ref p2, length - Vector512<ushort>.Count);

                while (Unsafe.IsAddressLessThan(ref p1, ref minusOneVec))
                {
                    p1Val = Vector512.LoadUnsafe(ref p1);
                    p2Val = Vector512.LoadUnsafe(ref p2);

                    p1Val = Vector512.ConditionalSelect(Vector512.Equals(p1Val, vComparand), vReplacement, p1Val);
                    p2Val = Vector512.ConditionalSelect(Vector512.Equals(p2Val, vComparand), vReplacement, p2Val);

                    if (p1Val != p2Val)
                    {
                        return false;
                    }

                    p1 = ref Unsafe.Add(ref p1, Vector512<ushort>.Count);
                    p2 = ref Unsafe.Add(ref p2, Vector512<ushort>.Count);
                }

                p1Val = Vector512.LoadUnsafe(ref minusOneVec);
                p2Val = Vector512.LoadUnsafe(ref minusOneVec2);

                p1Val = Vector512.ConditionalSelect(Vector512.Equals(p1Val, vComparand), vReplacement, p1Val);
                p2Val = Vector512.ConditionalSelect(Vector512.Equals(p2Val, vComparand), vReplacement, p2Val);

                if (p1Val != p2Val)
                {
                    return false;
                }
            }
            else if (Vector256.IsHardwareAccelerated && (uint)length >= (uint)Vector256<ushort>.Count)
            {
                var vComparand = Vector256.Create(comparand);
                var vReplacement = Vector256.Create(replacement);
                Vector256<ushort> p1Val, p2Val;

                ref ushort minusOneVec = ref Unsafe.Add(ref p1, length - Vector256<ushort>.Count);
                ref ushort minusOneVec2 = ref Unsafe.Add(ref p2, length - Vector256<ushort>.Count);

                while (Unsafe.IsAddressLessThan(ref p1, ref minusOneVec))
                {
                    p1Val = Vector256.LoadUnsafe(ref p1);
                    p2Val = Vector256.LoadUnsafe(ref p2);

                    p1Val = Vector256.ConditionalSelect(Vector256.Equals(p1Val, vComparand), vReplacement, p1Val);
                    p2Val = Vector256.ConditionalSelect(Vector256.Equals(p2Val, vComparand), vReplacement, p2Val);

                    if (p1Val != p2Val)
                    {
                        return false;
                    }

                    p1 = ref Unsafe.Add(ref p1, Vector256<ushort>.Count);
                    p2 = ref Unsafe.Add(ref p2, Vector256<ushort>.Count);
                }

                p1Val = Vector256.LoadUnsafe(ref minusOneVec);
                p2Val = Vector256.LoadUnsafe(ref minusOneVec2);

                p1Val = Vector256.ConditionalSelect(Vector256.Equals(p1Val, vComparand), vReplacement, p1Val);
                p2Val = Vector256.ConditionalSelect(Vector256.Equals(p2Val, vComparand), vReplacement, p2Val);

                if (p1Val != p2Val)
                {
                    return false;
                }
            }
            else if (Vector128.IsHardwareAccelerated && (uint)length >= (uint)Vector128<ushort>.Count)
            {
                var vComparand = Vector128.Create(comparand);
                var vReplacement = Vector128.Create(replacement);
                Vector128<ushort> p1Val, p2Val;

                ref ushort minusOneVec = ref Unsafe.Add(ref p1, length - Vector128<ushort>.Count);
                ref ushort minusOneVec2 = ref Unsafe.Add(ref p2, length - Vector128<ushort>.Count);

                while (Unsafe.IsAddressLessThan(ref p1, ref minusOneVec))
                {
                    p1Val = Vector128.LoadUnsafe(ref p1);
                    p2Val = Vector128.LoadUnsafe(ref p2);

                    p1Val = Vector128.ConditionalSelect(Vector128.Equals(p1Val, vComparand), vReplacement, p1Val);
                    p2Val = Vector128.ConditionalSelect(Vector128.Equals(p2Val, vComparand), vReplacement, p2Val);

                    if (p1Val != p2Val)
                    {
                        return false;
                    }

                    p1 = ref Unsafe.Add(ref p1, Vector128<ushort>.Count);
                    p2 = ref Unsafe.Add(ref p2, Vector128<ushort>.Count);
                }

                p1Val = Vector128.LoadUnsafe(ref minusOneVec);
                p2Val = Vector128.LoadUnsafe(ref minusOneVec2);

                p1Val = Vector128.ConditionalSelect(Vector128.Equals(p1Val, vComparand), vReplacement, p1Val);
                p2Val = Vector128.ConditionalSelect(Vector128.Equals(p2Val, vComparand), vReplacement, p2Val);

                if (p1Val != p2Val)
                {
                    return false;
                }
            }
            else
            {
                for (int i = 0; i < length; ++i)
                {
                    ushort p1Val = Unsafe.Add(ref p1, i);
                    ushort p2Val = Unsafe.Add(ref p2, i);

                    if (p1Val == comparand)
                        p1Val = replacement;

                    if (p2Val == comparand)
                        p2Val = replacement;

                    if (p1Val != p2Val)
                        return false;
                }
            }

            return true;
        }
    }

    public static bool SupportsSymlinks(string path)
    {
        path = Path.GetFullPath(path);

        var file = Path.Combine(path, Path.GetRandomFileName());
        var symlinkPath = Path.Combine(path, "testing");

        try
        {
            File.Create(file).Dispose();

            var symlink = File.CreateSymbolicLink(symlinkPath, file);

            return symlink.ResolveLinkTarget(false)?.FullName == file;
        }
        catch
        {
            return false;
        }
        finally
        {
            File.Delete(file);
            File.Delete(symlinkPath);
        }
    }

    /// <summary>
    /// Check if the platform is decomposing unicode data for us.  We will
    /// emulate core Git and prefer to use precomposed unicode data internally
    /// on these platforms, composing the decomposed unicode on the fly.
    /// </summary>
    /// <remarks>
    /// This mainly happens on the Mac where HDFS stores filenames as
    /// decomposed unicode.  Even on VFAT and SAMBA file systems, the Mac will
    /// return decomposed unicode from readdir() even when the actual
    /// filesystem is storing precomposed unicode.
    /// </remarks>
    /// <param name="root"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    internal static bool DoesDecomposeUnicode(string root)
    {
        const string nfc_file = "Åström";
        const string nfd_file = "A°stro\"m";

        string composed = PosixJoin(root, nfc_file);
        string decomposed = PosixJoin(root, nfd_file);

        Debug.Assert(!File.Exists(composed));
        Debug.Assert(!File.Exists(decomposed));

        using var handle = File.OpenHandle(composed, FileMode.CreateNew, FileAccess.ReadWrite, options: FileOptions.DeleteOnClose);

        Debug.Assert(File.Exists(composed));

        return File.Exists(decomposed);
    }

    [SupportedOSPlatform("windows6.1")]
    internal static unsafe FileAttributes? Win32GetFileAttributes(string path)
    {
        fixed (char* pPath = path)
        {
            uint attributes = Windows.GetFileAttributesW(pPath);

            return attributes == uint.MaxValue ? null : (FileAttributes)attributes;
        }
    }

    [SupportedOSPlatform("windows6.1")]
    internal static unsafe bool Win32Exists(string path)
    {
        Debug.Assert(!path.Contains('\0'));

        fixed (char* pPath = path)
        {
            return Windows.PathFileExistsW(pPath);
        }
    }

    private static readonly EnumerationOptions _copyDirOptions = new() { RecurseSubdirectories = true };

    internal enum CopyDirectoryFlags
    {
        None = 0,
        CreateEmptyDirectories  = 1,
        CopySymlinks            = 1 << 1,
        CopyDotFiles            = 1 << 2,
        Overwrite               = 1 << 3,
        ChangeModeDirectories   = 1 << 4,
        SimpleTargetMode            = 1 << 5,
        LinkFiles               = 1 << 6,
    }

    [ThreadStatic]
    private static FileStreamOptions? CopyRecursive_FileCreateOptions;

    // TODO: This method is a potentially major optimization opportunity
    private static void CopyRecursive_Core(string source, string target, CopyDirectoryFlags flags, UnixFileMode mode)
    {
        Debug.Assert(source != null);
        Debug.Assert(target != null);

        if ((flags & CopyDirectoryFlags.CopyDotFiles) == 0
            && Path.GetFileName(source.AsSpan()).StartsWith('.'))
            return;

        if (!Path.Exists(source))
        {
            throw new FileNotFoundException($"The file '{source}' does not exist!");
        }

        bool exists = Path.Exists(target);
        FileAttributes sourceAttributes = File.GetAttributes(source);
        FileAttributes targetAttributes = exists ? File.GetAttributes(target) : default;

        UnixFileMode sourceMode = !OperatingSystem.IsWindows() ? File.GetUnixFileMode(source) : default;

        if ((sourceAttributes & FileAttributes.Directory) != 0)
        {
            if ((flags & CopyDirectoryFlags.ChangeModeDirectories) == 0)
                mode = sourceMode;

            if (!exists && (flags & CopyDirectoryFlags.CreateEmptyDirectories) != 0)
                FileSystemHelpers.CreateDirectory(target, mode);

            if (!exists || (targetAttributes & FileAttributes.Directory) != 0)
            {
                foreach (var nextSource in Directory.EnumerateFileSystemEntries(source))
                {
                    var nextTarget = Path.Join(target, Path.GetFileName(nextSource.AsSpan()));

                    CopyRecursive_Core(nextSource, nextTarget, flags, mode);
                }
            }
        }
        else
        {
            if (exists)
            {
                if ((flags & CopyDirectoryFlags.Overwrite) == 0)
                    return;

                try
                {
                    File.Delete(target);
                }
                catch (Exception e)
                {
                    throw new Git2OSException($"Cannot overwrite existing file '{target}'!", e);
                }
            }

            if ((sourceAttributes & FileAttributes.Normal) == 0
                && ((sourceAttributes & FileAttributes.ReparsePoint) == 0 || (flags & CopyDirectoryFlags.CopySymlinks) == 0))
                return;

            if ((flags & CopyDirectoryFlags.CreateEmptyDirectories) == 0)
            {
                var parent = PathPool.GetOrAdd(Path.GetDirectoryName(target.AsSpan()));

                FileSystemHelpers.CreateDirectory(parent, mode);
            }

            if ((flags & CopyDirectoryFlags.LinkFiles) != 0)
            {
                File.CreateSymbolicLink(target, source);
            }
            else if ((sourceAttributes & FileAttributes.ReparsePoint) != 0 && File.ResolveLinkTarget(source, false) is { } link)
            {
                File.CreateSymbolicLink(target, link.FullName);
            }
            else
            {
                if (!OperatingSystem.IsWindows())
                { // create the file with the given filemode

                    // thread static/local
                    var targetOptions = CopyRecursive_FileCreateOptions ??= new FileStreamOptions()
                    {
                        Mode = FileMode.CreateNew,
                        Access = FileAccess.Write,
                        Share = FileShare.None
                    };

                    targetOptions.UnixCreateMode = (flags & CopyDirectoryFlags.SimpleTargetMode) != 0
                        ? ((sourceMode & (UnixFileMode)0x100) != 0 ? (UnixFileMode)0x1ff /*0777*/ : (UnixFileMode)0x1B6 /*0666*/)
                        : sourceMode;

                    using var sourceStream = File.Open(source, FileMode.Open, FileAccess.Read, FileShare.Read);
                    using var targetStream = File.Open(target, targetOptions);

                    sourceStream.CopyTo(targetStream);
                }
                else
                {
                    File.Copy(source, target);
                }
            }
        }
    }

    internal static void CopyRecursive(string source, string target, CopyDirectoryFlags flags, UnixFileMode mode)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(target);

        source = Path.GetFullPath(source);
        target = Path.GetFullPath(target);

        if (source == target)
            return;

        CopyRecursive_Core(source, target, flags, mode);
    }

    [SupportedOSPlatform("windows")]
    private static unsafe void CopyRecursive_Win32(string source, string target, CopyDirectoryFlags flags)
    {
        FileAttributes sourceAttributes;
        fixed (char* pSource = source)
        {
            sourceAttributes = (FileAttributes)Windows.GetFileAttributesW(pSource);
        }

        if ((int)sourceAttributes == -1)
            throw Marshal.GetLastWin32Error() == ERROR.ERROR_FILE_NOT_FOUND
                ? new FileNotFoundException()
                : Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error())!;

        FileAttributes targetAttributes;
        fixed (char* pTarget = target)
        {
            targetAttributes = (FileAttributes)Windows.GetFileAttributesW(pTarget);
        }
        
        if ((int)targetAttributes == -1 && (sourceAttributes & FileAttributes.Directory) != 0)
        {
            Directory.CreateDirectory(target);
        }

        var enumerable = new FileSystemEnumerable<(string, FileAttributes)>(source, (ref entry) => (entry.ToFullPath(), entry.Attributes), _copyDirOptions)
        {
            ShouldIncludePredicate = (ref entry) =>
            {
                if ((flags & CopyDirectoryFlags.CreateEmptyDirectories) == 0 && entry.IsDirectory)
                    return false;

                return !entry.FileName.StartsWith('.') || (flags & CopyDirectoryFlags.CopyDotFiles) != 0;
            },
            ShouldRecursePredicate = (ref entry) =>
            {
                return !entry.FileName.StartsWith('.') || (flags & CopyDirectoryFlags.CopyDotFiles) != 0;
            }
        };

        foreach (var (sourcePath, attributes) in enumerable)
        {
            string targetPath = Path.Join(target, sourcePath.AsSpan(source.Length));

            sourceAttributes = attributes;

            fixed (char* pTarget = targetPath)
            {
                targetAttributes = (FileAttributes)Windows.GetFileAttributesW(pTarget);
            }

            bool exists = (uint)targetAttributes != Windows.INVALID_FILE_ATTRIBUTES;

            if (!exists && Marshal.GetLastWin32Error() != ERROR.ERROR_FILE_NOT_FOUND)
            {
                throw Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error())!;
            }

            if ((sourceAttributes & FileAttributes.Directory) != 0)
            {
                // is the destination a file?
                if (exists)
                    continue;

                Directory.CreateDirectory(targetPath);
            }
            else
            {
                if (exists)
                {
                    if ((flags & CopyDirectoryFlags.Overwrite) == 0)
                        continue;

                    try
                    {
                        File.Delete(targetPath);
                    }
                    catch (Exception e)
                    {
                        throw new Git2OSException($"Cannot overwrite file '{targetPath}'!", e);
                    }
                }

                if ((sourceAttributes & FileAttributes.Normal) == 0
                    && ((sourceAttributes & FileAttributes.ReparsePoint) == 0 || (flags & CopyDirectoryFlags.CopySymlinks) == 0))
                    continue;

                Directory.CreateDirectory(
                    PathPool.GetOrAdd(
                        Path.GetDirectoryName(targetPath.AsSpan()))!);

                if ((flags & CopyDirectoryFlags.LinkFiles) != 0)
                {
                    File.CreateSymbolicLink(targetPath, sourcePath);
                }
                else if ((sourceAttributes & FileAttributes.ReparsePoint) != 0 && File.ResolveLinkTarget(sourcePath, false) is { } link)
                {
                    File.CreateSymbolicLink(targetPath, link.FullName);
                }
                else
                {
                    File.Copy(sourcePath, targetPath);
                }
            }
        }
    }

    private static void CopyRecursive_Unix(string source, string target, CopyDirectoryFlags flags, UnixFileMode mode)
    {
        source = Path.GetFullPath(source);

        if (!Directory.Exists(source))
            return;

        target = Path.GetFullPath(target);

        if (!Directory.Exists(target))
        {
            var td = Directory.CreateDirectory(target);

            if (!OperatingSystem.IsWindows())
                td.UnixFileMode = mode;
        }

        var enumerable = new FileSystemEnumerable<string>(source, (ref entry) => entry.ToFullPath(), _copyDirOptions)
        {
            ShouldIncludePredicate = (ref entry) =>
            {
                if ((flags & CopyDirectoryFlags.CreateEmptyDirectories) == 0 && entry.IsDirectory)
                    return false;

                return !entry.FileName.StartsWith('.') || (flags & CopyDirectoryFlags.CopyDotFiles) != 0;
            },
            ShouldRecursePredicate = (ref entry) =>
            {
                return !entry.FileName.StartsWith('.') || (flags & CopyDirectoryFlags.CopyDotFiles) != 0;
            }
        };

        foreach (var sourcePath in enumerable)
        {
            string targetPath = Path.Join(target, sourcePath.AsSpan(source.Length));

            var sourceAttributes = File.GetAttributes(sourcePath);

            if ((sourceAttributes & FileAttributes.Directory) != 0)
            {
                bool exists = Path.Exists(targetPath);

                // is the destination a file?
                if (!exists || (File.GetAttributes(targetPath) & FileAttributes.Directory) == 0)
                    continue;

                Directory.CreateDirectory(targetPath);
            }
            else
            {
                if (Path.Exists(targetPath))
                {
                    if ((flags & CopyDirectoryFlags.Overwrite) == 0)
                        continue;

                    File.Delete(targetPath);
                }

                if ((sourceAttributes & FileAttributes.Normal) == 0
                    && ((sourceAttributes & FileAttributes.ReparsePoint) == 0 || (flags & CopyDirectoryFlags.CopySymlinks) == 0))
                    continue;

                var targetDir = Path.GetDirectoryName(targetPath)!;
                if (!Directory.Exists(targetDir))
                    Directory.CreateDirectory(targetDir);

                if ((flags & CopyDirectoryFlags.LinkFiles) != 0)
                {
                    File.CreateSymbolicLink(targetPath, sourcePath);
                }
                else if ((sourceAttributes & FileAttributes.ReparsePoint) != 0 && File.ResolveLinkTarget(sourcePath, false) is { } link)
                {
                    File.CreateSymbolicLink(targetPath, link.FullName);
                }
                else
                {
                    File.Copy(sourcePath, targetPath);

                    File.SetUnixFileMode(targetPath, mode);
                }
            }
        }
    }

    internal static bool EndsInFileName(ReadOnlySpan<char> path, ReadOnlySpan<char> filename)
    {
        return Path.GetFileName(Path.TrimEndingDirectorySeparator(path)).SequenceEqual(filename);
    }
}
